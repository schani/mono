namespace System.Text {

	public partial class StringBuilder {

        internal byte[] m_ChunkBytes;
		internal bool m_IsCompact;

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
					if (value.IsCompact) {
						m_ChunkBytes = new byte[capacity * sizeof(byte)];
						ThreadSafeCopyBytes((byte*)valueChars + startIndex, m_ChunkBytes, 0, length);
					} else {
						m_ChunkBytes = new byte[capacity * sizeof(char)];
						ThreadSafeCopy(valueChars + startIndex, (char*)m_ChunkBytes, 0, length);
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
            m_ChunkChars = new char[persistedCapacity];
			m_IsCompact = persistedString.IsCompact;
			if (m_IsCompact) {
				unsafe {
					fixed (char* persistedChars = persistedString) {
						ThreadSafeCopyBytes((byte*)persistedChars);
					}
				}
				persistedString.CopyTo(0, m_ChunkChars, 0, persistedString.Length);
			} else {
				persistedString.CopyTo(0, m_ChunkChars, 0, persistedString.Length);
			}
            m_ChunkLength = persistedString.Length;
            m_ChunkPrevious = null;
            VerifyClassInvariant();
        }
#endif

	}

}
