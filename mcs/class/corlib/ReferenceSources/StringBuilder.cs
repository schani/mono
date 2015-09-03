namespace System.Text {

    using System.Text;
    using System.Runtime;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Threading;
    using System.Globalization;
    using System.Diagnostics.Contracts;

	public partial class StringBuilder {

        internal byte[] m_ChunkBytes;
		internal bool m_IsCompact;

		internal int CharSize {
			get { return m_IsCompact ? sizeof(byte) : sizeof(char); }
		}

		internal bool CompactRepresentable {
			get { return m_IsCompact && (m_ChunkPrevious == null || m_ChunkPrevious.CompactRepresentable); }
		}

		private unsafe void Degrade() {
			if (!m_IsCompact)
				return;
			byte[] newArray = new byte[m_ChunkBytes.Length * sizeof(char)];
			fixed (byte* destBytes = newArray)
				for (int i = 0; i < m_ChunkLength; ++i)
					((char*)destBytes)[i] = (char)m_ChunkBytes[i];
			m_ChunkBytes = newArray;
			m_IsCompact = false;
		}

		private int ChunkCapacity {
			get { return m_ChunkBytes.Length / CharSize; }
		}

		// ----------------------------------------
		// Ports of mscorlib methods follow.
		// ----------------------------------------

        // Creates a new string builder from the specifed substring with the specified
        // capacity.  The maximum number of characters is set by capacity.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder(String value, int startIndex, int length, int capacity) {
            if (capacity<0) {
                throw new ArgumentOutOfRangeException("capacity",
                                                      Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", "capacity"));
            }
            if (length<0) {
                throw new ArgumentOutOfRangeException("length",
                                                      Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "length"));
            }
            if (startIndex<0) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }
            Contract.EndContractBlock();

            if (value == null) {
                value = String.Empty;
            }
            if (startIndex > value.Length - length) {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
            }
            m_MaxCapacity = Int32.MaxValue;
            if (capacity == 0) {
                capacity = DefaultCapacity;
            }
            if (capacity < length)
                capacity = length;

            m_ChunkLength = length;
			m_IsCompact = value.IsCompact;

			unsafe {
				fixed (char* valueChars = value) {
					int charSize = value.CharSize;
					m_ChunkBytes = new byte[capacity * charSize];
					fixed (byte* chunkBytes = m_ChunkBytes) {
						/* FIXME: Not thread-safe. */
						Buffer.Memcpy(chunkBytes, (byte*)valueChars + startIndex * charSize, length * charSize);
					}
				}
			}

        }

        // Creates an empty StringBuilder with a minimum capacity of capacity
        // and a maximum capacity of maxCapacity.
        public StringBuilder(int capacity, int maxCapacity) {
            if (capacity>maxCapacity) {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
            }
            if (maxCapacity<1) {
                throw new ArgumentOutOfRangeException("maxCapacity", Environment.GetResourceString("ArgumentOutOfRange_SmallMaxCapacity"));
            }
            if (capacity<0) {
                throw new ArgumentOutOfRangeException("capacity",
                                                      Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", "capacity"));
            }
            Contract.EndContractBlock();

            if (capacity == 0) {
                capacity = Math.Min(DefaultCapacity, maxCapacity);
            }

            m_MaxCapacity = maxCapacity;
            m_ChunkBytes = new byte[capacity];
			m_IsCompact = true;
        }

#if !MONO
#if FEATURE_SERIALIZATION
        [System.Security.SecurityCritical]  // auto-generated
        private StringBuilder(SerializationInfo info, StreamingContext context) {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            int persistedCapacity = 0;
            string persistedString = null;
            int persistedMaxCapacity = Int32.MaxValue;
            bool capacityPresent = false;

            // Get the data
            SerializationInfoEnumerator enumerator = info.GetEnumerator();
            while (enumerator.MoveNext()) {
                switch (enumerator.Name) {
                    case MaxCapacityField:
                        persistedMaxCapacity = info.GetInt32(MaxCapacityField);
                        break;
                    case StringValueField:
                        persistedString = info.GetString(StringValueField);
                        break;
                    case CapacityField:
                        persistedCapacity = info.GetInt32(CapacityField);
                        capacityPresent = true;
                        break;
                    default:
                        // Ignore other fields for forward compatability.
                        break;
                }

            }

            // Check values and set defaults
            if (persistedString == null) {
                persistedString = String.Empty;
            }
            if (persistedMaxCapacity < 1 || persistedString.Length > persistedMaxCapacity) {
                throw new SerializationException(Environment.GetResourceString("Serialization_StringBuilderMaxCapacity"));
            }

            if (!capacityPresent) {
                // StringBuilder in V1.X did not persist the Capacity, so this is a valid legacy code path.
                persistedCapacity = DefaultCapacity;
                if (persistedCapacity < persistedString.Length) {
                    persistedCapacity = persistedString.Length;
                }
                if (persistedCapacity > persistedMaxCapacity) {
                    persistedCapacity = persistedMaxCapacity;
                }
            }
            if (persistedCapacity < 0 || persistedCapacity < persistedString.Length || persistedCapacity > persistedMaxCapacity) {
                throw new SerializationException(Environment.GetResourceString("Serialization_StringBuilderCapacity"));
            }

            // Assign
            m_MaxCapacity = persistedMaxCapacity;
			m_IsCompact = persistedString.CompactRepresentable();
            m_ChunkBytes = new byte[persistedCapacity * (m_IsCompact ? sizeof(byte) : sizeof(char))];
			unsafe {
				fixed (byte* chunkBytes = m_ChunkBytes)
				fixed (char* persistedChars = persistedString) {
					if (persistedString.IsCompact) {
						/* FIXME: Not thread-safe. */
						Buffer.Memcpy(chunkBytes, (byte*)persistedChars, persistedString.Length);
					} else if (m_IsCompact) {
						/* FIXME: Unroll. */
						for (int i = 0; i < persistedString.Length; ++i)
							chunkBytes[i] = (byte)persistedChars[i];
					} else {
						/* FIXME: Unroll. */
						for (int i = 0; i < persistedString.Length; ++i)
							((char*)chunkBytes)[i] = persistedChars[i];
					}
				}
			}
            m_ChunkLength = persistedString.Length;
            m_ChunkPrevious = null;
            VerifyClassInvariant();
        }
#endif
#else
        [System.Security.SecurityCritical]  // auto-generated
        private StringBuilder(SerializationInfo info, StreamingContext context) {
			throw new NotImplementedException("StringBuilder(SerializationInfo,StreamingContext)");
		}
#endif

        [System.Diagnostics.Conditional("_DEBUG")]
        private void VerifyClassInvariant() {
            BCLDebug.Correctness((uint)(m_ChunkOffset + ChunkCapacity) >= m_ChunkOffset, "Integer Overflow");
            StringBuilder currentBlock = this;
            int maxCapacity = this.m_MaxCapacity;
            for (; ; )
            {
                // All blocks have copy of the maxCapacity.
                Contract.Assert(currentBlock.m_MaxCapacity == maxCapacity, "Bad maxCapacity");
                Contract.Assert(currentBlock.m_ChunkBytes != null, "Empty Buffer");

                Contract.Assert(currentBlock.m_ChunkLength <= currentBlock.m_ChunkBytes.Length, "Out of range length");
				Contract.Assert(m_ChunkLength * CharSize == currentBlock.m_ChunkBytes.Length, "Out of range length");
                Contract.Assert(currentBlock.m_ChunkLength >= 0, "Negative length");
                Contract.Assert(currentBlock.m_ChunkOffset >= 0, "Negative offset");

                StringBuilder prevBlock = currentBlock.m_ChunkPrevious;
                if (prevBlock == null)
                {
                    Contract.Assert(currentBlock.m_ChunkOffset == 0, "First chunk's offset is not 0");
                    break;
                }
                // There are no gaps in the blocks. 
                Contract.Assert(currentBlock.m_ChunkOffset == prevBlock.m_ChunkOffset + prevBlock.m_ChunkLength, "There is a gap between chunks!");
                currentBlock = prevBlock;
            }
        }

        public int Capacity {
#if !FEATURE_CORECLR
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            get { return ChunkCapacity + m_ChunkOffset; }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
                }
                if (value > MaxCapacity) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
                }
                if (value < Length) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }
                Contract.EndContractBlock();

                if (Capacity == value)
					return;

				int newLen = value - m_ChunkOffset;
				int charSize = CharSize;
				byte[] newArray = new byte[newLen * charSize];
				Array.Copy(m_ChunkBytes, newArray, m_ChunkLength * charSize);
				m_ChunkBytes = newArray;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe override String ToString() {
            Contract.Ensures(Contract.Result<String>() != null);

            VerifyClassInvariant();
            
            if (Length == 0)
                return String.Empty;

			bool compactRepresentable = CompactRepresentable;
            string ret = string.FastAllocateString(Length, String.SelectEncoding(compactRepresentable));
            StringBuilder chunk = this;
			fixed (char* destChars = ret) {
				if (compactRepresentable) {
					byte* destBytes = (byte*)destChars;
                    do {
                        if (chunk.m_ChunkLength > 0) {
                            // Copy these into local variables so that they are stable even in the presence of ----s (hackers might do this)
                            byte[] sourceArray = chunk.m_ChunkBytes;
                            int chunkOffset = chunk.m_ChunkOffset;
                            int chunkLength = chunk.m_ChunkLength;
    
                            // Check that we will not overrun our boundaries. 
                            if ((uint)(chunkLength + chunkOffset) <= ret.Length && (uint)chunkLength <= (uint)sourceArray.Length) {
                                fixed (byte* sourceBytes = sourceArray)
                                    Buffer.Memcpy(destBytes + chunkOffset, sourceBytes, chunkLength);
                            } else {
                                throw new ArgumentOutOfRangeException("chunkLength", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                            }
                        }
                        chunk = chunk.m_ChunkPrevious;
                    } while (chunk != null);
				} else {
                    do {
                        if (chunk.m_ChunkLength > 0) {
                            // Copy these into local variables so that they are stable even in the presence of ----s (hackers might do this)
                            byte[] sourceArray = chunk.m_ChunkBytes;
                            int chunkOffset = chunk.m_ChunkOffset;
                            int chunkLength = chunk.m_ChunkLength;
    
                            // Check that we will not overrun our boundaries. 
                            if ((uint)(chunkLength + chunkOffset) <= ret.Length && (uint)chunkLength <= (uint)sourceArray.Length) {
								fixed (byte* sourceBytes = sourceArray) {
									if (chunk.m_IsCompact) {
										/* FIXME: Unroll. */
										for (int i = 0; i < chunkLength; ++i)
											destChars[chunkOffset + i] = (char)sourceBytes[i];
									} else {
										String.wstrcpy(destChars + chunkOffset, (char*)sourceBytes, chunkLength);
									}
								}
                            } else {
                                throw new ArgumentOutOfRangeException("chunkLength", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                            }
                        }
                        chunk = chunk.m_ChunkPrevious;
                    } while (chunk != null);
				}
			}
            return ret;
        }

        // Converts a substring of this string builder to a String.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe String ToString(int startIndex, int length) {
            Contract.Ensures(Contract.Result<String>() != null);

            int currentLength = this.Length;
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }
            if (startIndex > currentLength)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndexLargerThanLength"));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }
            if (startIndex > (currentLength - length))
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
            }

            VerifyClassInvariant();

            StringBuilder chunk = this;
            int sourceEndIndex = startIndex + length;

			bool compactRepresentable = CompactRepresentable;
            string ret = string.FastAllocateString(length, String.SelectEncoding(compactRepresentable));
            int curDestIndex = length;
			fixed (char* destChars = ret)
			{
				if (compactRepresentable) {
					byte* destBytes = (byte*)destChars;

					while (curDestIndex > 0)
					{
						int chunkEndIndex = sourceEndIndex - chunk.m_ChunkOffset;
						if (chunkEndIndex >= 0)
						{
							if (chunkEndIndex > chunk.m_ChunkLength)
								chunkEndIndex = chunk.m_ChunkLength;
    
							int countLeft = curDestIndex;
							int chunkCount = countLeft;
							int chunkStartIndex = chunkEndIndex - countLeft;
							if (chunkStartIndex < 0)
							{
								chunkCount += chunkStartIndex;
								chunkStartIndex = 0;
							}
							curDestIndex -= chunkCount;
    
							if (chunkCount > 0)
							{
								// work off of local variables so that they are stable even in the presence of ----s (hackers might do this)
								byte[] sourceArray = chunk.m_ChunkBytes;
    
								// Check that we will not overrun our boundaries. 
								if ((uint)(chunkCount + curDestIndex) <= length && (uint)(chunkCount + chunkStartIndex) <= (uint)sourceArray.Length)
								{
									fixed (byte* sourceBytes = sourceArray)
										/* It's safe to copy directly from the source array because
										 * all chunks must be compact in order for the StringBuffer
										 * to be CompactRepresentable.
										 */
										Buffer.Memcpy(destBytes + curDestIndex, sourceBytes + chunkStartIndex, chunkCount);
								}
								else
								{
									throw new ArgumentOutOfRangeException("chunkCount", Environment.GetResourceString("ArgumentOutOfRange_Index"));
								}
							}
						}
						chunk = chunk.m_ChunkPrevious;
					}
				} else {
					while (curDestIndex > 0)
					{
						int chunkEndIndex = sourceEndIndex - chunk.m_ChunkOffset;
						if (chunkEndIndex >= 0)
						{
							if (chunkEndIndex > chunk.m_ChunkLength)
								chunkEndIndex = chunk.m_ChunkLength;
    
							int countLeft = curDestIndex;
							int chunkCount = countLeft;
							int chunkStartIndex = chunkEndIndex - countLeft;
							if (chunkStartIndex < 0)
							{
								chunkCount += chunkStartIndex;
								chunkStartIndex = 0;
							}
							curDestIndex -= chunkCount;
    
							if (chunkCount > 0)
							{
								// work off of local variables so that they are stable even in the presence of ----s (hackers might do this)
								byte[] sourceArray = chunk.m_ChunkBytes;
    
								// Check that we will not overrun our boundaries. 
								if ((uint)(chunkCount + curDestIndex) <= length && (uint)(chunkCount + chunkStartIndex) <= (uint)sourceArray.Length)
								{
									fixed (byte* sourceBytes = sourceArray) {
										if (chunk.m_IsCompact) {
											/* FIXME: Unroll. */
											for (int i = 0; i < chunkCount; ++i)
												destChars[curDestIndex + i] = (char)sourceBytes[chunkStartIndex + i];
										} else {
											string.wstrcpy(destChars + curDestIndex, (char*)sourceBytes + chunkStartIndex, chunkCount);
										}
									}
								}
								else
								{
									throw new ArgumentOutOfRangeException("chunkCount", Environment.GetResourceString("ArgumentOutOfRange_Index"));
								}
							}
						}
						chunk = chunk.m_ChunkPrevious;
					}
				}
			}
            return ret;
        }

        public int Length {
#if !FEATURE_CORECLR
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return m_ChunkOffset + m_ChunkLength;
            }
            set {
                //If the new length is less than 0 or greater than our Maximum capacity, bail.
                if (value<0) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
                }

                if (value>MaxCapacity) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }
                Contract.EndContractBlock();

                int originalCapacity = Capacity;

                if (value == 0 && m_ChunkPrevious == null)
                {
                    m_ChunkLength = 0;
                    m_ChunkOffset = 0;
                    Contract.Assert(Capacity >= originalCapacity, "setting the Length should never decrease the Capacity");
                    return;
                }

                int delta = value - Length;
                // if the specified length is greater than the current length
                if (delta > 0)
                {
                    // the end of the string value of the current StringBuilder object is padded with the Unicode NULL character
                    Append('\0', delta);        // We could improve on this, but who does this anyway?
                }
                // if the specified length is less than or equal to the current length
                else
                {
                    StringBuilder chunk = FindChunkForIndex(value);
                    if (chunk != this)
                    {
                        // we crossed a chunk boundary when reducing the Length, we must replace this middle-chunk with a new
                        // larger chunk to ensure the original capacity is preserved

						// Before:
						//
						// previous    chunk       this
						// +--------+  +--------+  +--------+
						// |ABCDABCD|<-|ABCDABCD|<-|ABCD....|
						// +--------+  +--------+  +--------+
						//
						// After:
						//
						// previous    this
						// +--------+  +----------------+
						// |ABCDABCD|<-|ABCD............|
						// +--------+  +----------------+

                        int newLen = originalCapacity - chunk.m_ChunkOffset;
						int charSize = chunk.CharSize;
                        byte[] newArray = new byte[newLen * charSize];

                        Contract.Assert(newLen > chunk.m_ChunkBytes.Length, "the new chunk should be larger than the one it is replacing");
						
						Array.Copy(chunk.m_ChunkBytes, newArray, chunk.m_ChunkLength * charSize);
                        m_ChunkBytes = newArray;
                        m_IsCompact = chunk.m_IsCompact;
                        m_ChunkPrevious = chunk.m_ChunkPrevious;                        
                        m_ChunkOffset = chunk.m_ChunkOffset;
                    }
                    m_ChunkLength = value - chunk.m_ChunkOffset;
                    VerifyClassInvariant();
                }
                Contract.Assert(Capacity >= originalCapacity, "setting the Length should never decrease the Capacity");
            }
        }

        [System.Runtime.CompilerServices.IndexerName("Chars")]
        public unsafe char this[int index] {
            // 

            get {
                StringBuilder chunk = this;
                for (; ; )
                {
                    int indexInBlock = index - chunk.m_ChunkOffset;
                    if (indexInBlock >= 0)
                    {
                        if (indexInBlock >= chunk.m_ChunkLength)
                            throw new IndexOutOfRangeException();
						fixed (byte* chunkBytes = chunk.m_ChunkBytes)
							return chunk.m_IsCompact ? (char)chunkBytes[indexInBlock] : ((char*)chunkBytes)[indexInBlock];
                    }
                    chunk = chunk.m_ChunkPrevious;
                    if (chunk == null)
                        throw new IndexOutOfRangeException();
                }
            }
            set {
				if (!String.CompactRepresentable(value))
					Degrade();
                StringBuilder chunk = this;
                for (; ; )
                {
                    int indexInBlock = index - chunk.m_ChunkOffset;
                    if (indexInBlock >= 0)
                    {
                        if (indexInBlock >= chunk.m_ChunkLength)
                            throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
						fixed (byte* chunkBytes = chunk.m_ChunkBytes) {
							if (chunk.m_IsCompact)
								chunkBytes[indexInBlock] = (byte)value;
							else
								((char*)chunkBytes)[indexInBlock] = value;
						}
                        return;
                    }
                    chunk = chunk.m_ChunkPrevious;
                    if (chunk == null)
                        throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }
            }
        }

        // Appends a character at the end of this string builder. The capacity is adjusted as needed.
        public unsafe StringBuilder Append(char value, int repeatCount) {
			if (!String.CompactRepresentable(value))
				Degrade();
            if (repeatCount<0) {
                throw new ArgumentOutOfRangeException("repeatCount", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            }
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();

            if (repeatCount==0) {
                return this;
            }
            int idx = m_ChunkLength;
            while (repeatCount > 0)
            {
                if (idx < ChunkCapacity)
                {
					fixed (byte* chunkBytes = m_ChunkBytes) {
						if (m_IsCompact)
							chunkBytes[idx++] = (byte)value;
						else
							((char*)chunkBytes)[idx++] = value;
					}
                    --repeatCount;
                }
                else
                {
                    m_ChunkLength = idx;
                    ExpandByABlock(repeatCount);
                    Contract.Assert(m_ChunkLength == 0, "Expand should create a new block");
                    idx = 0;
                }
            }
            m_ChunkLength = idx;
            VerifyClassInvariant();
            return this;
        }

#if !MONO
        // Appends a copy of this string at the end of this string builder.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public StringBuilder Append(String value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            if (value != null) {
                // This is a hand specialization of the 'AppendHelper' code below. 
                // We could have just called AppendHelper.  
                char[] chunkChars = m_ChunkChars;
                int chunkLength = m_ChunkLength;
                int valueLen = value.Length;
                int newCurrentIndex = chunkLength + valueLen;
                if (newCurrentIndex < chunkChars.Length)    // Use strictly < to avoid issue if count == 0, newIndex == length
                {
                    if (valueLen <= 2)
                    {
                        if (valueLen > 0)
                            chunkChars[chunkLength] = value[0];
                        if (valueLen > 1)
                            chunkChars[chunkLength + 1] = value[1];
                    }
                    else
                    {
                        unsafe {
                            fixed (char* valuePtr = value)
                            fixed (char* destPtr = &chunkChars[chunkLength]) {
#if MONO
                                if (value.IsCompact) {
                                    /* FIXME: Unroll. */
                                    for (int i = 0; i < valueLen; ++i)
                                        destPtr[i] = (char)((byte*)valuePtr)[i];
                                } else
#endif
                                {
                                    string.wstrcpy(destPtr, valuePtr, valueLen);
                                }
                            }
                        }
                    }
                    m_ChunkLength = newCurrentIndex;
                }
                else
                    AppendHelper(value);
            }
            return this;
        }
#else
        // Appends a copy of this string at the end of this string builder.
        [System.Security.SecuritySafeCritical]  // auto-generated
		public StringBuilder Append(String value) {
			throw new NotImplementedException("Append(String)");
		}
#endif

#if !MONO
        [System.Runtime.InteropServices.ComVisible(false)]
        [SecuritySafeCritical]
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) {
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("Arg_NegativeArgCount"));
            }

            if (destinationIndex < 0) {
                throw new ArgumentOutOfRangeException("destinationIndex",
                    Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "destinationIndex"));
            }

            if (destinationIndex > destination.Length - count) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_OffsetOut"));
            }

            if ((uint)sourceIndex > (uint)Length) {
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (sourceIndex > Length - count) {
                throw new ArgumentException(Environment.GetResourceString("Arg_LongerThanSrcString"));
            }
            Contract.EndContractBlock();

            VerifyClassInvariant();

            StringBuilder chunk = this;
            int sourceEndIndex = sourceIndex + count;
            int curDestIndex = destinationIndex + count;
            while (count > 0)
            {
                int chunkEndIndex = sourceEndIndex - chunk.m_ChunkOffset;
                if (chunkEndIndex >= 0)
                {
                    if (chunkEndIndex > chunk.m_ChunkLength)
                        chunkEndIndex = chunk.m_ChunkLength;

                    int chunkCount = count;
                    int chunkStartIndex = chunkEndIndex - count;
                    if (chunkStartIndex < 0)
                    {
                        chunkCount += chunkStartIndex;
                        chunkStartIndex = 0;
                    }
                    curDestIndex -= chunkCount;
                    count -= chunkCount;

                    // SafeCritical: we ensure that chunkStartIndex + chunkCount are within range of m_chunkChars
                    // as well as ensuring that curDestIndex + chunkCount are within range of destination
                    ThreadSafeCopy(chunk.m_ChunkChars, chunkStartIndex, destination, curDestIndex, chunkCount);
                }
                chunk = chunk.m_ChunkPrevious;
            }
        }
#else
        [System.Runtime.InteropServices.ComVisible(false)]
        [SecuritySafeCritical]
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) {
			throw new NotImplementedException("CopyTo(int,char[],int,int)");
		}
#endif

        // Appends a character at the end of this string builder. The capacity is adjusted as needed.
        public unsafe StringBuilder Append(char value) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
			if (!String.CompactRepresentable(value))
				Degrade();

            if (m_ChunkLength < ChunkCapacity) {
				fixed (byte* chunkBytes = m_ChunkBytes) {
					if (m_IsCompact)
						chunkBytes[m_ChunkLength++] = (byte)value;
					else
						((char*)chunkBytes)[m_ChunkLength++] = value;
				}
            } else
                Append(value, 1);
            return this;
        }

#if !MONO
        public bool Equals(StringBuilder sb) 
        {
            if (sb == null)
                return false;
            if (Capacity != sb.Capacity || MaxCapacity != sb.MaxCapacity || Length != sb.Length)
                return false;
            if (sb == this)
                return true;

            StringBuilder thisChunk = this;
            int thisChunkIndex = thisChunk.m_ChunkLength;
            StringBuilder sbChunk = sb;
            int sbChunkIndex = sbChunk.m_ChunkLength;
            for (; ; )
            {
                // Decrement the pointer to the 'this' StringBuilder
                --thisChunkIndex;
                --sbChunkIndex;

                while (thisChunkIndex < 0)
                {
                    thisChunk = thisChunk.m_ChunkPrevious;
                    if (thisChunk == null)
                        break;
                    thisChunkIndex = thisChunk.m_ChunkLength + thisChunkIndex;
                }

                // Decrement the pointer to the 'this' StringBuilder
                while (sbChunkIndex < 0)
                {
                    sbChunk = sbChunk.m_ChunkPrevious;
                    if (sbChunk == null)
                        break;
                    sbChunkIndex = sbChunk.m_ChunkLength + sbChunkIndex;
                }

                if (thisChunkIndex < 0)
                    return sbChunkIndex < 0;
                if (sbChunkIndex < 0)
                    return false;
                if (thisChunk.m_ChunkChars[thisChunkIndex] != sbChunk.m_ChunkChars[sbChunkIndex])
                    return false;
            }
        }
#else
        public bool Equals(StringBuilder sb) 
        {
			throw new NotImplementedException("Equals(StringBuilder)");
		}
#endif

#if !MONO
        public StringBuilder Replace(char oldChar, char newChar, int startIndex, int count) {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);

            int currentLength = Length;
            if ((uint)startIndex > (uint)currentLength) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0 || startIndex > currentLength - count) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            int endIndex = startIndex + count;
            StringBuilder chunk = this;
            for (; ; )
            {
                int endIndexInChunk = endIndex - chunk.m_ChunkOffset;
                int startIndexInChunk = startIndex - chunk.m_ChunkOffset;
                if (endIndexInChunk >= 0)
                {
                    int curInChunk = Math.Max(startIndexInChunk, 0);
                    int endInChunk = Math.Min(chunk.m_ChunkLength, endIndexInChunk);
                    while (curInChunk < endInChunk)
                    {
                        if (chunk.m_ChunkChars[curInChunk] == oldChar)
                            chunk.m_ChunkChars[curInChunk] = newChar;
                        curInChunk++;
                    }
                }
                if (startIndexInChunk >= 0)
                    break;
                chunk = chunk.m_ChunkPrevious;
            }
            return this;
        }
#else
        public StringBuilder Replace(char oldChar, char newChar, int startIndex, int count) {
			throw new NotImplementedException("Replace(char,char,int,int)");
		}
#endif

        /// <summary>
        /// Appends 'value' of length 'count' to the stringBuilder. 
        /// </summary>
        [SecurityCritical]
        internal unsafe StringBuilder Append(char* value, int valueCount)
        {
            Contract.Assert(value != null, "Value can't be null");
            Contract.Assert(valueCount >= 0, "Count can't be negative");
			if (!String.CompactRepresentable(value, valueCount))
				Degrade();

            // This case is so common we want to optimize for it heavily. 
            int newIndex = valueCount + m_ChunkLength;
            if (newIndex <= ChunkCapacity)
            {
				fixed (byte* chunkBytes = m_ChunkBytes) {
					if (m_IsCompact) {
						/* FIXME: Unroll. */
						for (int i = 0; i < valueCount; ++i)
							chunkBytes[m_ChunkLength + i] = (byte)value[i];
					} else {
						/* FIXME: Not thread-safe. */
						String.wstrcpy((char*)chunkBytes + m_ChunkLength, value, valueCount);
					}
				}
                m_ChunkLength = newIndex;
            }
            else
            {
                // Copy the first chunk
                int firstLength = ChunkCapacity - m_ChunkLength;
                if (firstLength > 0)
                {
					fixed (byte* chunkBytes = m_ChunkBytes) {
						if (m_IsCompact) {
							/* FIXME: Unroll. */
							for (int i = 0; i < firstLength; ++i)
								chunkBytes[m_ChunkLength + i] = (byte)value[i];
						} else {
							/* FIXME: Not thread-safe. */
							String.wstrcpy((char*)chunkBytes + m_ChunkLength, value, firstLength);
						}
					}
                    m_ChunkLength = ChunkCapacity;
                }

                // Expand the builder to add another chunk. 
                int restLength = valueCount - firstLength;
                ExpandByABlock(restLength);
                Contract.Assert(m_ChunkLength == 0, "Expand did not make a new block");

                // Copy the second chunk
				fixed (byte* chunkBytes = m_ChunkBytes) {
					if (m_IsCompact) {
						/* FIXME: Unroll. */
						for (int i = 0; i < restLength; ++i)
							chunkBytes[firstLength + i] = (byte)(value + firstLength)[i];
					} else {
						String.wstrcpy((char*)chunkBytes, value + firstLength, restLength);
					}
				}
                m_ChunkLength = restLength;
            }
            VerifyClassInvariant();
            return this;
        }

#if !MONO
        /// <summary>
        /// Appends 'value' of length 'count' to the stringBuilder. 
        /// </summary>
        [SecurityCritical]
        internal unsafe StringBuilder Append(byte* value, int valueCount)
        {
            Contract.Assert(value != null, "Value can't be null");
            Contract.Assert(valueCount >= 0, "Count can't be negative");

            // This case is so common we want to optimize for it heavily. 
            int newIndex = valueCount + m_ChunkLength;
            if (newIndex <= m_ChunkChars.Length)
            {
                ThreadSafeCopy(value, m_ChunkChars, m_ChunkLength, valueCount);
                m_ChunkLength = newIndex;
            }
            else
            {
                // Copy the first chunk
                int firstLength = m_ChunkChars.Length - m_ChunkLength;
                if (firstLength > 0)
                {
                    ThreadSafeCopy(value, m_ChunkChars, m_ChunkLength, firstLength);
                    m_ChunkLength = m_ChunkChars.Length;
                }

                // Expand the builder to add another chunk. 
                int restLength = valueCount - firstLength;
                ExpandByABlock(restLength);
                Contract.Assert(m_ChunkLength == 0, "Expand did not make a new block");

                // Copy the second chunk
                ThreadSafeCopy(value + firstLength, m_ChunkChars, 0, restLength);
                m_ChunkLength = restLength;
            }
            VerifyClassInvariant();
            return this;
        }
#else
        internal unsafe StringBuilder Append(byte* value, int valueCount) {
			throw new NotImplementedException("Append(byte*,int)");
		}
#endif

#if !MONO
        /// <summary>
        /// 'replacements' is a list of index (relative to the begining of the 'chunk' to remove
        /// 'removeCount' characters and replace them with 'value'.   This routine does all those 
        /// replacements in bulk (and therefore very efficiently. 
        /// with the string 'value'.  
        /// </summary>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void ReplaceAllInChunk(int[] replacements, int replacementsCount, StringBuilder sourceChunk, int removeCount, string value)
        {
            if (replacementsCount <= 0)
                return;

            unsafe {
                fixed (char* valuePtr = value)
                {
                    // calculate the total amount of extra space or space needed for all the replacements.  
                    int delta = (value.Length - removeCount) * replacementsCount;
    
                    StringBuilder targetChunk = sourceChunk;        // the target as we copy chars down
                    int targetIndexInChunk = replacements[0];
    
                    // Make the room needed for all the new characters if needed. 
                    if (delta > 0)
                        MakeRoom(targetChunk.m_ChunkOffset + targetIndexInChunk, delta, out targetChunk, out targetIndexInChunk, true);
                    // We made certain that characters after the insertion point are not moved, 
                    int i = 0;
                    for (; ; )
                    {
                        // Copy in the new string for the ith replacement
#if MONO
						if (value.IsCompact)
							ReplaceInPlaceAtChunk(ref targetChunk, ref targetIndexInChunk, (byte*)valuePtr, value.Length);
						else
#endif
							ReplaceInPlaceAtChunk(ref targetChunk, ref targetIndexInChunk, valuePtr, value.Length);
                        int gapStart = replacements[i] + removeCount;
                        i++;
                        if (i >= replacementsCount)
                            break;
    
                        int gapEnd = replacements[i];
                        Contract.Assert(gapStart < sourceChunk.m_ChunkChars.Length, "gap starts at end of buffer.  Should not happen");
                        Contract.Assert(gapStart <= gapEnd, "negative gap size");
                        Contract.Assert(gapEnd <= sourceChunk.m_ChunkLength, "gap too big");
                        if (delta != 0)     // can skip the sliding of gaps if source an target string are the same size.  
                        {
                            // Copy the gap data between the current replacement and the the next replacement
                            fixed (char* sourcePtr = &sourceChunk.m_ChunkChars[gapStart])
                                ReplaceInPlaceAtChunk(ref targetChunk, ref targetIndexInChunk, sourcePtr, gapEnd - gapStart);
                        }
                        else
                        {
                            targetIndexInChunk += gapEnd - gapStart;
                            Contract.Assert(targetIndexInChunk <= targetChunk.m_ChunkLength, "gap not in chunk");
                        }
                    }
    
                    // Remove extra space if necessary. 
                    if (delta < 0)
                        Remove(targetChunk.m_ChunkOffset + targetIndexInChunk, -delta, out targetChunk, out targetIndexInChunk);
                }
            }
        }
#else
        private void ReplaceAllInChunk(int[] replacements, int replacementsCount, StringBuilder sourceChunk, int removeCount, string value) {
			throw new NotImplementedException("ReplaceAllInChunk(int[],int,StringBuilder,int,String)");
		}
#endif

#if !MONO
        /// <summary>
        /// Returns true if the string that is starts at 'chunk' and 'indexInChunk, and has a logical
        /// length of 'count' starts with the string 'value'. 
        /// </summary>
        private bool StartsWith(StringBuilder chunk, int indexInChunk, int count, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (count == 0)
                    return false;
                if (indexInChunk >= chunk.m_ChunkLength)
                {
                    chunk = Next(chunk);
                    if (chunk == null)
                        return false;
                    indexInChunk = 0;
                }

                // See if there no match, break out of the inner for loop
                if (value[i] != chunk.m_ChunkChars[indexInChunk])
                    return false;

                indexInChunk++;
                --count;
            }
            return true;
        }
#else
        private bool StartsWith(StringBuilder chunk, int indexInChunk, int count, string value) {
			throw new NotImplementedException("StartsWith");
		}
#endif

#if !MONO
        /// <summary>
        /// ReplaceInPlaceAtChunk is the logical equivalent of 'memcpy'.  Given a chunk and ann index in
        /// that chunk, it copies in 'count' characters from 'value' and updates 'chunk, and indexInChunk to 
        /// point at the end of the characters just copyied (thus you can splice in strings from multiple 
        /// places by calling this mulitple times.  
        /// </summary>
        [SecurityCritical]
        unsafe private void ReplaceInPlaceAtChunk(ref StringBuilder chunk, ref int indexInChunk, char* value, int count)
        {
            if (count != 0)
            {
                for (; ; )
                {
                    int lengthInChunk = chunk.m_ChunkLength - indexInChunk;
                    Contract.Assert(lengthInChunk >= 0, "index not in chunk");

                    int lengthToCopy = Math.Min(lengthInChunk, count);
                    ThreadSafeCopy(value, chunk.m_ChunkChars, indexInChunk, lengthToCopy);

                    // Advance the index. 
                    indexInChunk += lengthToCopy;
                    if (indexInChunk >= chunk.m_ChunkLength)
                    {
                        chunk = Next(chunk);
                        indexInChunk = 0;
                    }
                    count -= lengthToCopy;
                    if (count == 0)
                        break;
                    value += lengthToCopy;
                }
            }
        }
#else
        unsafe private void ReplaceInPlaceAtChunk(ref StringBuilder chunk, ref int indexInChunk, char* value, int count) {
			throw new NotImplementedException("ReplaceInPlaceAtChunk(StringBuilder&,int&,char*,int)");
		}
#endif

#if !MONO
        [SecurityCritical]
        unsafe private void ReplaceInPlaceAtChunk(ref StringBuilder chunk, ref int indexInChunk, byte* value, int count)
        {
            if (count != 0)
            {
                for (; ; )
                {
                    int lengthInChunk = chunk.m_ChunkLength - indexInChunk;
                    Contract.Assert(lengthInChunk >= 0, "index not in chunk");

                    int lengthToCopy = Math.Min(lengthInChunk, count);
                    ThreadSafeCopy(value, chunk.m_ChunkChars, indexInChunk, lengthToCopy);

                    // Advance the index. 
                    indexInChunk += lengthToCopy;
                    if (indexInChunk >= chunk.m_ChunkLength)
                    {
                        chunk = Next(chunk);
                        indexInChunk = 0;
                    }
                    count -= lengthToCopy;
                    if (count == 0)
                        break;
                    value += lengthToCopy;
                }
            }
        }
#else
        unsafe private void ReplaceInPlaceAtChunk(ref StringBuilder chunk, ref int indexInChunk, byte* value, int count) {
			throw new NotImplementedException("ReplaceInPlaceAtChunk(StringBuilder&,int&,byte*,int)");
		}
#endif

#if !MONO
         // Copies the source StringBuilder to the destination IntPtr memory allocated with len bytes.
        #if !FEATURE_CORECLR
        [System.Runtime.ForceTokenStabilization]
        #endif //!FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe void InternalCopy(IntPtr dest, int len) {
            if(len ==0)
                return;

            bool isLastChunk = true;
            byte* dstPtr = (byte*) dest.ToPointer();
            StringBuilder currentSrc = FindChunkForByte(len);

            do {
                int chunkOffsetInBytes = currentSrc.m_ChunkOffset*sizeof(char);
                int chunkLengthInBytes = currentSrc.m_ChunkLength*sizeof(char);
                fixed(char* charPtr = &currentSrc.m_ChunkChars[0]) {
                    byte* srcPtr = (byte*) charPtr;
                    if(isLastChunk) {
                        isLastChunk= false;
                        Buffer.Memcpy(dstPtr + chunkOffsetInBytes, srcPtr, len - chunkOffsetInBytes);
                    } else {
                        Buffer.Memcpy(dstPtr + chunkOffsetInBytes, srcPtr, chunkLengthInBytes);
                    }
                }
                currentSrc = currentSrc.m_ChunkPrevious;
            } while(currentSrc != null);
        }
#else
         // Copies the source StringBuilder to the destination IntPtr memory allocated with len bytes.
        #if !FEATURE_CORECLR
        [System.Runtime.ForceTokenStabilization]
        #endif //!FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe void InternalCopy(IntPtr dest, int len) {
			throw new NotImplementedException("InternalCopy(IntPtr,int)");
		}
#endif

        /// <summary>
        /// Assumes that 'this' is the last chunk in the list and that it is full.  Upon return the 'this'
        /// block is updated so that it is a new block that has at least 'minBlockCharCount' characters.
        /// that can be used to copy characters into it.   
        /// </summary>
        private void ExpandByABlock(int minBlockCharCount)
        {
            Contract.Requires(Capacity == Length, "Expand expect to be called only when there is no space left");        // We are currently full
            Contract.Requires(minBlockCharCount > 0, "Expansion request must be positive");

            VerifyClassInvariant();

            if ((minBlockCharCount + Length) > m_MaxCapacity)
                throw new ArgumentOutOfRangeException("requiredLength", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));

            // Compute the length of the new block we need 
            // We make the new chunk at least big enough for the current need (minBlockCharCount)
            // But also as big as the current length (thus doubling capacity), up to a maximum
            // (so we stay in the small object heap, and never allocate really big chunks even if
            // the string gets really big. 
            int newBlockLength = Math.Max(minBlockCharCount, Math.Min(Length, MaxChunkSize));

            // Copy the current block to the new block, and initialize this to point at the new buffer. 
            m_ChunkPrevious = new StringBuilder(this);
            m_ChunkOffset += m_ChunkLength;
            m_ChunkLength = 0;

            // Check for integer overflow (logical buffer size > int.MaxInt)
            if (m_ChunkOffset + newBlockLength < newBlockLength)
            {
                m_ChunkBytes = null;
                throw new OutOfMemoryException();
            }
			/* Optimistically assume this chunk will be compact. */
            m_ChunkBytes = new byte[newBlockLength];
			m_IsCompact = true;

            VerifyClassInvariant();
        }

        /// <summary>
        /// Used by ExpandByABlock to create a new chunk.  The new chunk is a copied from 'from'
        /// In particular the buffer is shared.  It is expected that 'from' chunk (which represents
        /// the whole list, is then updated to point to point to this new chunk. 
        /// </summary>
        private StringBuilder(StringBuilder from)
        {
            m_ChunkLength = from.m_ChunkLength;
            m_ChunkOffset = from.m_ChunkOffset;
            m_ChunkBytes = from.m_ChunkBytes;
            m_IsCompact = from.m_IsCompact;
            m_ChunkPrevious = from.m_ChunkPrevious;
            m_MaxCapacity = from.m_MaxCapacity;
            VerifyClassInvariant();
        }

#if !MONO
        /// <summary>
        /// Creates a gap of size 'count' at the logical offset (count of characters in the whole string
        /// builder) 'index'.  It returns the 'chunk' and 'indexInChunk' which represents a pointer to
        /// this gap that was just created.  You can then use 'ReplaceInPlaceAtChunk' to fill in the
        /// chunk
        ///
        /// ReplaceAllChunks relies on the fact that indexes above 'index' are NOT moved outside 'chunk'
        /// by this process (because we make the space by creating the cap BEFORE the chunk).  If we
        /// change this ReplaceAllChunks needs to be updated. 
        ///
        /// If dontMoveFollowingChars is true, then the room must be made by inserting a chunk BEFORE the
        /// current chunk (this is what it does most of the time anyway)
        /// </summary>
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void MakeRoom(int index, int count, out StringBuilder chunk, out int indexInChunk, bool doneMoveFollowingChars)
        {
            VerifyClassInvariant();
            Contract.Assert(count > 0, "Count must be strictly positive");
            Contract.Assert(index >= 0, "Index can't be negative");
            if (count + Length > m_MaxCapacity)
                throw new ArgumentOutOfRangeException("requiredLength", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));

            chunk = this;
            while (chunk.m_ChunkOffset > index)
            {
                chunk.m_ChunkOffset += count;
                chunk = chunk.m_ChunkPrevious;
            }
            indexInChunk = index - chunk.m_ChunkOffset;

            // Cool, we have some space in this block, and you don't have to copy much to get it, go ahead
            // and use it.  This happens typically  when you repeatedly insert small strings at a spot
            // (typically the absolute front) of the buffer.    
            if (!doneMoveFollowingChars && chunk.m_ChunkLength <= DefaultCapacity * 2 && chunk.m_ChunkChars.Length - chunk.m_ChunkLength >= count)
            {
                for (int i = chunk.m_ChunkLength; i > indexInChunk; )
                {
                    --i;
                    chunk.m_ChunkChars[i + count] = chunk.m_ChunkChars[i];
                }
                chunk.m_ChunkLength += count;
                return;
            }

            // Allocate space for the new chunk (will go before this one)
            StringBuilder newChunk = new StringBuilder(Math.Max(count, DefaultCapacity), chunk.m_MaxCapacity, chunk.m_ChunkPrevious);
            newChunk.m_ChunkLength = count;

            // Copy the head of the buffer to the  new buffer. 
            int copyCount1 = Math.Min(count, indexInChunk);
            if (copyCount1 > 0)
            {
                unsafe {
                    fixed (char* chunkCharsPtr = chunk.m_ChunkChars) {
                        ThreadSafeCopy(chunkCharsPtr, newChunk.m_ChunkChars, 0, copyCount1);
    
                        // Slide characters in the current buffer over to make room. 
                        int copyCount2 = indexInChunk - copyCount1;
                        if (copyCount2 >= 0)
                        {
                            ThreadSafeCopy(chunkCharsPtr + copyCount1, chunk.m_ChunkChars, 0, copyCount2);
                            indexInChunk = copyCount2;
                        }
                    }
                }
            }

            chunk.m_ChunkPrevious = newChunk;           // Wire in the new chunk
            chunk.m_ChunkOffset += count;
            if (copyCount1 < count)
            {
                chunk = newChunk;
                indexInChunk = copyCount1;
            }

            VerifyClassInvariant();
        }
#else
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void MakeRoom(int index, int count, out StringBuilder chunk, out int indexInChunk, bool doneMoveFollowingChars) {
			throw new NotImplementedException("MakeRoom(int index, int count, out StringBuilder chunk, out int indexInChunk, bool doneMoveFollowingChars)");
		}
#endif

#if !MONO
        /// <summary>
        ///  Used by MakeRoom to allocate another chunk.  
        /// </summary>
        private StringBuilder(int size, int maxCapacity, StringBuilder previousBlock)
        {
            Contract.Assert(size > 0, "size not positive");
            Contract.Assert(maxCapacity > 0, "maxCapacity not positive");
            m_ChunkChars = new char[size];
            m_MaxCapacity = maxCapacity;
            m_ChunkPrevious = previousBlock;
            if (previousBlock != null)
                m_ChunkOffset = previousBlock.m_ChunkOffset + previousBlock.m_ChunkLength;
            VerifyClassInvariant();
        }
#else
        private StringBuilder(int size, int maxCapacity, StringBuilder previousBlock) {
			throw new NotImplementedException("StringBuilder(int size, int maxCapacity, StringBuilder previousBlock)");
		}
#endif

#if !MONO
        /// <summary>
        /// Removes 'count' characters from the logical index 'startIndex' and returns the chunk and 
        /// index in the chunk of that logical index in the out parameters.  
        /// </summary>
        [SecuritySafeCritical]
        private void Remove(int startIndex, int count, out StringBuilder chunk, out int indexInChunk)
        {
            VerifyClassInvariant();
            Contract.Assert(startIndex >= 0 && startIndex < Length, "startIndex not in string");

            int endIndex = startIndex + count;

            // Find the chunks for the start and end of the block to delete. 
            chunk = this;
            StringBuilder endChunk = null;
            int endIndexInChunk = 0;
            for (; ; )
            {
                if (endIndex - chunk.m_ChunkOffset >= 0)
                {
                    if (endChunk == null)
                    {
                        endChunk = chunk;
                        endIndexInChunk = endIndex - endChunk.m_ChunkOffset;
                    }
                    if (startIndex - chunk.m_ChunkOffset >= 0)
                    {
                        indexInChunk = startIndex - chunk.m_ChunkOffset;
                        break;
                    }
                }
                else
                {
                    chunk.m_ChunkOffset -= count;
                }
                chunk = chunk.m_ChunkPrevious;
            }
            Contract.Assert(chunk != null, "fell off beginning of string!");

            int copyTargetIndexInChunk = indexInChunk;
            int copyCount = endChunk.m_ChunkLength - endIndexInChunk;
            if (endChunk != chunk)
            {
                copyTargetIndexInChunk = 0;
                // Remove the characters after startIndex to end of the chunk
                chunk.m_ChunkLength = indexInChunk;

                // Remove the characters in chunks between start and end chunk
                endChunk.m_ChunkPrevious = chunk;
                endChunk.m_ChunkOffset = chunk.m_ChunkOffset + chunk.m_ChunkLength;

                // If the start is 0 then we can throw away the whole start chunk
                if (indexInChunk == 0)
                {
                    endChunk.m_ChunkPrevious = chunk.m_ChunkPrevious;
                    chunk = endChunk;
                }
            }
            endChunk.m_ChunkLength -= (endIndexInChunk - copyTargetIndexInChunk);

            // SafeCritical: We ensure that endIndexInChunk + copyCount is within range of m_ChunkChars and
            // also ensure that copyTargetIndexInChunk + copyCount is within the chunk
            //
            // Remove any characters in the end chunk, by sliding the characters down. 
            if (copyTargetIndexInChunk != endIndexInChunk)  // Sometimes no move is necessary
                ThreadSafeCopy(endChunk.m_ChunkChars, endIndexInChunk, endChunk.m_ChunkChars, copyTargetIndexInChunk, copyCount);

            Contract.Assert(chunk != null, "fell off beginning of string!");
            VerifyClassInvariant();
        }
#else
        [SecuritySafeCritical]
        private void Remove(int startIndex, int count, out StringBuilder chunk, out int indexInChunk) {
			throw new NotImplementedException("Remove(int startIndex, int count, out StringBuilder chunk, out int indexInChunk)");
		}
#endif

#if FEATURE_SERIALIZATION
        [System.Security.SecurityCritical]  // auto-generated
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();

            VerifyClassInvariant();
            info.AddValue(MaxCapacityField, m_MaxCapacity);
            info.AddValue(CapacityField, Capacity);
            info.AddValue(StringValueField, ToString());
            // Note: persist "m_currentThread" to be compatible with old versions
            info.AddValue(ThreadIDField, 0);
        }
#endif

	}

}
