//
// System.String.cs
//
// Authors:
//   Patrik Torstensson
//   Jeffrey Stedfast (fejj@ximian.com)
//   Dan Lewis (dihlewis@yahoo.co.uk)
//   Sebastien Pouliot  <sebastien@ximian.com>
//   Marek Safar (marek.safar@seznam.cz)
//   Andreas Nahr (Classdevelopment@A-SoftTech.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004-2005 Novell (http://www.novell.com)
// Copyright (c) 2012 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Text;
using System.Diagnostics.Contracts;

namespace System
{
	partial class String
	{

		[NonSerialized]private int m_taggedStringLength;
		[NonSerialized]private byte m_firstByte;

		internal const int ENCODING_UTF16 = 0;
		internal const int ENCODING_ASCII = 1;

		public int Length {
			get { return (int)(m_taggedStringLength >> 1); }
		}

		internal bool IsCompact {
			get { return (m_taggedStringLength & 1) != 0; }
		}

		private static int SelectEncoding(bool compact)
		{
			return compact ? ENCODING_ASCII : ENCODING_UTF16;
		}

		private bool CompactRepresentable(char value)
		{
			return (int)value <= 0x7F;
		}

		private bool CompactRepresentable(char [] value)
		{
			for (int i = 0; i < value.Length; ++i)
				if (!CompactRepresentable(value[i]))
					return false;
			return true;
		}

		private unsafe bool CompactRepresentable(char* value, int length)
		{
			for (int i = 0; i < length; ++i)
				if (!CompactRepresentable(value[i]))
					return false;
			return true;
		}

		internal static unsafe int CompareOrdinalUnchecked (String strA, int indexA, int lenA, String strB, int indexB, int lenB)
		{
			if (strA == null) {
				return strB == null ? 0 : -1;
			}
			if (strB == null) {
				return 1;
			}
			int lengthA = Math.Min (lenA, strA.Length - indexA);
			int lengthB = Math.Min (lenB, strB.Length - indexB);

			if (lengthA == lengthB && indexA == indexB && Object.ReferenceEquals (strA, strB))
				return 0;

			fixed (char* aptr = strA, bptr = strB) {
				if (strA.IsCompact && strB.IsCompact) {
					byte* ap = (byte*)aptr + indexA;
					byte* end = ap + Math.Min (lengthA, lengthB);
					byte* bp = (byte*)bptr + indexB;
					while (ap < end) {
						if (*ap != *bp)
							return *ap - *bp;
						ap++;
						bp++;
					}
					return lengthA - lengthB;
				}
				if (strA.IsCompact) {
					byte* ap = (byte*)aptr + indexA;
					byte* end = ap + Math.Min (lengthA, lengthB);
					char* bp = bptr + indexB;
					while (ap < end) {
						if ((char)*ap != *bp)
							return *ap - *bp;
						ap++;
						bp++;
					}
					return lengthA - lengthB;
				}
				if (strB.IsCompact) {
					char* ap = (char*)aptr + indexA;
					char* end = ap + Math.Min (lengthA, lengthB);
					byte* bp = (byte*)bptr + indexB;
					while (ap < end) {
						if (*ap != (char)*bp)
							return *ap - *bp;
						ap++;
						bp++;
					}
					return lengthA - lengthB;
				}
				{
					char* ap = aptr + indexA;
					char* end = ap + Math.Min (lengthA, lengthB);
					char* bp = bptr + indexB;
					while (ap < end) {
						if (*ap != *bp)
							return *ap - *bp;
						ap++;
						bp++;
					}
					return lengthA - lengthB;
				}
			}
		}

		public int IndexOf (char value, int startIndex, int count)
		{
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ("startIndex", "Cannot be negative and must be< 0");
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "< 0");
			if (startIndex > this.Length - count)
				throw new ArgumentOutOfRangeException ("count", "startIndex + count > this.Length");

			if ((startIndex == 0 && this.Length == 0) || (startIndex == this.Length) || (count == 0))
				return -1;

			return IndexOfUnchecked (value, startIndex, count);
		}

		internal unsafe int IndexOfUnchecked (char value, int startIndex, int count)
		{
			// It helps JIT compiler to optimize comparison
			int value_32 = (int)value;

			fixed (byte* startByte = &m_firstByte) {
				if (IsCompact) {
					/* FIXME: Unroll. */
					for (int i = startIndex; i < startIndex + count; ++i)
						if ((char)startByte [i] == value)
							return i;
				} else {
					char* start = (char*)startByte;
					char* ptr = start + startIndex;
					char* end_ptr = ptr + (count >> 3 << 3);
					while (ptr != end_ptr) {
						if (*ptr == value_32)
							return (int)(ptr - start);
						if (ptr[1] == value_32)
							return (int)(ptr - start + 1);
						if (ptr[2] == value_32)
							return (int)(ptr - start + 2);
						if (ptr[3] == value_32)
							return (int)(ptr - start + 3);
						if (ptr[4] == value_32)
							return (int)(ptr - start + 4);
						if (ptr[5] == value_32)
							return (int)(ptr - start + 5);
						if (ptr[6] == value_32)
							return (int)(ptr - start + 6);
						if (ptr[7] == value_32)
							return (int)(ptr - start + 7);

						ptr += 8;
					}
					end_ptr += count & 0x07;
					while (ptr != end_ptr) {
						if (*ptr == value_32)
							return (int)(ptr - start);
						ptr++;
					}
				}
				return -1;
			}
		}

		internal unsafe int IndexOfUnchecked (string value, int startIndex, int count)
		{
			int valueLen = value.Length;
			if (count < valueLen)
				return -1;

			if (valueLen <= 1) {
				if (valueLen == 1)
					return IndexOfUnchecked (value[0], startIndex, count);
				return startIndex;
			}

			fixed (byte* thisPtrByte = &m_firstByte)
			fixed (char* valuePtr = value) {
				char* thisPtr = (char*)thisPtrByte;
				if (IsCompact && value.IsCompact) {
					byte* ap = (byte*)thisPtr + startIndex;
					byte* thisEnd = ap + count - valueLen + 1;
					while (ap != thisEnd) {
						if (*ap == *(byte*)valuePtr) {
							for (int i = 1; i < valueLen; i++)
								if (ap[i] != ((byte*)valuePtr)[i])
									goto NextVal;
							return (int)(ap - thisPtr);
						}
						NextVal:
						ap++;
					}
				} else if (IsCompact) {
					byte* ap = (byte*)thisPtr + startIndex;
					byte* thisEnd = ap + count - valueLen + 1;
					while (ap != thisEnd) {
						if ((char)*ap == *valuePtr) {
							for (int i = 1; i < valueLen; i++)
								if ((char)ap[i] != valuePtr[i])
									goto NextVal;
							return (int)(ap - thisPtr);
						}
						NextVal:
						ap++;
					}
				} else if (value.IsCompact) {
					char* ap = thisPtr + startIndex;
					char* thisEnd = ap + count - valueLen + 1;
					while (ap != thisEnd) {
						if (*ap == (char)*(byte*)valuePtr) {
							for (int i = 1; i < valueLen; i++)
								if (ap[i] != (char)((byte*)valuePtr)[i])
									goto NextVal;
							return (int)(ap - thisPtr);
						}
						NextVal:
						ap++;
					}
				} else {
					char* ap = thisPtr + startIndex;
					char* thisEnd = ap + count - valueLen + 1;
					while (ap != thisEnd) {
						if (*ap == *valuePtr) {
							for (int i = 1; i < valueLen; i++)
								if (ap[i] != valuePtr[i])
									goto NextVal;
							return (int)(ap - thisPtr);
						}
						NextVal:
						ap++;
					}
				}
			}
			return -1;
		}

		public int IndexOfAny (char [] anyOf, int startIndex, int count)
		{
			if (anyOf == null)
				throw new ArgumentNullException ();
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException ();
			if (count < 0 || startIndex > this.Length - count)
				throw new ArgumentOutOfRangeException ("count", "Count cannot be negative, and startIndex + count must be less than Length of the string.");

			return IndexOfAnyUnchecked (anyOf, startIndex, count);
		}		

		unsafe int IndexOfAnyUnchecked (char[] anyOf, int startIndex, int count)
		{
			if (anyOf.Length == 0)
				return -1;

			if (anyOf.Length == 1)
				return IndexOfUnchecked (anyOf[0], startIndex, count);

			fixed (char* any = anyOf) {
				int highest = *any;
				int lowest = *any;

				char* end_any_ptr = any + anyOf.Length;
				char* any_ptr = any;
				while (++any_ptr != end_any_ptr) {
					if (*any_ptr > highest) {
						highest = *any_ptr;
						continue;
					}

					if (*any_ptr < lowest)
						lowest = *any_ptr;
				}

				fixed (byte* startByte = &m_firstByte) {
					char* start = (char*)startByte;
					if (IsCompact) {
						byte* ptr = (byte*)start + startIndex;
						byte* end_ptr = ptr + count;
						while (ptr != end_ptr) {
							if (*ptr > highest || *ptr < lowest) {
								ptr++;
								continue;
							}
							if ((char)*ptr == *any)
								return (int)(ptr - (byte*)start);
							any_ptr = any;
							while (++any_ptr != end_any_ptr) {
								if ((char)*ptr == *any_ptr)
									return (int)(ptr - (byte*)start);
							}
							ptr++;
						}
					} else {
						char* ptr = start + startIndex;
						char* end_ptr = ptr + count;
						while (ptr != end_ptr) {
							if (*ptr > highest || *ptr < lowest) {
								ptr++;
								continue;
							}
							if (*ptr == *any)
								return (int)(ptr - start);
							any_ptr = any;
							while (++any_ptr != end_any_ptr) {
								if (*ptr == *any_ptr)
									return (int)(ptr - start);
							}
							ptr++;
						}
					}
				}
			}
			return -1;
		}

		public int LastIndexOf (char value, int startIndex, int count)
		{
			if (this.Length == 0)
				return -1;
 
			// >= for char (> for string)
			if ((startIndex < 0) || (startIndex >= this.Length))
				throw new ArgumentOutOfRangeException ("startIndex", "< 0 || >= this.Length");
			if ((count < 0) || (count > this.Length))
				throw new ArgumentOutOfRangeException ("count", "< 0 || > this.Length");
			if (startIndex - count + 1 < 0)
				throw new ArgumentOutOfRangeException ("startIndex - count + 1 < 0");

			return LastIndexOfUnchecked (value, startIndex, count);
		}

		internal unsafe int LastIndexOfUnchecked (char value, int startIndex, int count)
		{
			// It helps JIT compiler to optimize comparison
			int value_32 = (int)value;

			fixed (byte* startByte = &m_firstByte) {
				char* start = (char*)startByte;
				if (IsCompact) {
					byte* ptr = (byte*)start + startIndex;
					byte* end_ptr = (byte*)ptr - (count & ~7);
					while (ptr != end_ptr) {
						if (*ptr == value_32)
							return (int)(ptr - (byte*)start);
						if (ptr[-1] == value_32)
							return (int)(ptr - (byte*)start) - 1;
						if (ptr[-2] == value_32)
							return (int)(ptr - (byte*)start) - 2;
						if (ptr[-3] == value_32)
							return (int)(ptr - (byte*)start) - 3;
						if (ptr[-4] == value_32)
							return (int)(ptr - (byte*)start) - 4;
						if (ptr[-5] == value_32)
							return (int)(ptr - (byte*)start) - 5;
						if (ptr[-6] == value_32)
							return (int)(ptr - (byte*)start) - 6;
						if (ptr[-7] == value_32)
							return (int)(ptr - (byte*)start) - 7;
						ptr -= 8;
					}
					end_ptr -= count & 7;
					while (ptr != end_ptr) {
						if (*ptr == value_32)
							return (int)(ptr - (byte*)start);
						ptr--;
					}
				} else {
					char* ptr = start + startIndex;
					char* end_ptr = ptr - (count & ~7);
					while (ptr != end_ptr) {
						if (*ptr == value_32)
							return (int)(ptr - start);
						if (ptr[-1] == value_32)
							return (int)(ptr - start) - 1;
						if (ptr[-2] == value_32)
							return (int)(ptr - start) - 2;
						if (ptr[-3] == value_32)
							return (int)(ptr - start) - 3;
						if (ptr[-4] == value_32)
							return (int)(ptr - start) - 4;
						if (ptr[-5] == value_32)
							return (int)(ptr - start) - 5;
						if (ptr[-6] == value_32)
							return (int)(ptr - start) - 6;
						if (ptr[-7] == value_32)
							return (int)(ptr - start) - 7;
						ptr -= 8;
					}
					end_ptr -= count & 7;
					while (ptr != end_ptr) {
						if (*ptr == value_32)
							return (int)(ptr - start);
						ptr--;
					}
				}
				return -1;
			}
		}

		public int LastIndexOfAny (char [] anyOf, int startIndex, int count)
		{
			if (anyOf == null) 
				throw new ArgumentNullException ();
			if (this.Length == 0)
				return -1;

			if ((startIndex < 0) || (startIndex >= this.Length))
				throw new ArgumentOutOfRangeException ("startIndex", "< 0 || > this.Length");
			if ((count < 0) || (count > this.Length))
				throw new ArgumentOutOfRangeException ("count", "< 0 || > this.Length");
			if (startIndex - count + 1 < 0)
				throw new ArgumentOutOfRangeException ("startIndex - count + 1 < 0");

			if (this.Length == 0)
				return -1;

			return LastIndexOfAnyUnchecked (anyOf, startIndex, count);
		}

		private unsafe int LastIndexOfAnyUnchecked (char [] anyOf, int startIndex, int count)
		{
			if (anyOf.Length == 1)
				return LastIndexOfUnchecked (anyOf[0], startIndex, count);

			fixed (byte* startByte = &m_firstByte)
			fixed (char* testStart = anyOf) {
				char* start = (char*)startByte;
				char* test;
				char* testEnd = testStart + anyOf.Length;
				if (IsCompact) {
					byte* ptr = (byte*)start + startIndex;
					byte* ptrEnd = ptr - count;
					while (ptr != ptrEnd) {
						test = testStart;
						while (test != testEnd) {
							if (*test == (char)*ptr)
								return (int)(ptr - (byte*)start);
							test++;
						}
						ptr--;
					}
				} else {
					char* ptr = start + startIndex;
					char* ptrEnd = ptr - count;
					while (ptr != ptrEnd) {
						test = testStart;
						while (test != testEnd) {
							if (*test == *ptr)
								return (int)(ptr - start);
							test++;
						}
						ptr--;
					}
				}
				return -1;
			}
		}

		internal static int nativeCompareOrdinalEx (String strA, int indexA, String strB, int indexB, int count)
		{
			//
			// .net does following checks in unmanaged land only which is quite
			// wrong as it's not always necessary and argument names don't match
			// but we are compatible
			//
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));

			if (indexA < 0 || indexA > strA.Length)
				throw new ArgumentOutOfRangeException("indexA", Environment.GetResourceString("ArgumentOutOfRange_Index"));

			if (indexB < 0 || indexB > strB.Length)
				throw new ArgumentOutOfRangeException("indexB", Environment.GetResourceString("ArgumentOutOfRange_Index"));

			return CompareOrdinalUnchecked (strA, indexA, count, strB, indexB, count);
		}

		unsafe String ReplaceInternal (char oldChar, char newChar)
		{
#if !BOOTSTRAP_BASIC			
			if (this.Length == 0 || oldChar == newChar)
				return this;
#endif
			int start_pos = IndexOfUnchecked (oldChar, 0, this.Length);
#if !BOOTSTRAP_BASIC
			if (start_pos == -1)
				return this;
#endif
			if (start_pos < 4)
				start_pos = 0;

			bool compact = IsCompact && (int)newChar <= 0x7F;
			string tmp = FastAllocateString (Length, compact ? ENCODING_ASCII : ENCODING_UTF16);
			fixed (byte* srcByte = &m_firstByte) {
				fixed (byte* destByte = &tmp.m_firstByte) {
					if (compact) {
						if (start_pos != 0)
							memcpy(destByte, srcByte, start_pos);
						byte* end_ptr = destByte + Length;
						byte* dest_ptr = destByte + start_pos;
						byte* src_ptr = srcByte + start_pos;
						while (dest_ptr != end_ptr) {
							if ((char)*src_ptr == oldChar)
								*dest_ptr = (byte)newChar;
							else
								*dest_ptr = *src_ptr;
							++src_ptr;
							++dest_ptr;
						}
					} else {
						char* dest = (char*)destByte;
						char* src = (char*)srcByte;
						if (start_pos != 0)
							CharCopy (dest, src, start_pos);
						char* end_ptr = dest + Length;
						char* dest_ptr = dest + start_pos;
						char* src_ptr = src + start_pos;
						while (dest_ptr != end_ptr) {
							if (*src_ptr == oldChar)
								*dest_ptr = newChar;
							else
								*dest_ptr = *src_ptr;
							++src_ptr;
							++dest_ptr;
						}
					}
				}
			}
			return tmp;
		}

		internal String ReplaceInternal (String oldValue, String newValue)
		{
			// LAMESPEC: According to MSDN the following method is culture-sensitive but this seems to be incorrect
			// LAMESPEC: Result is undefined if result Length is longer than maximum string Length

			if (oldValue == null)
				throw new ArgumentNullException ("oldValue");

			if (oldValue.Length == 0)
				throw new ArgumentException ("oldValue is the empty string.");

			if (this.Length == 0)
#if BOOTSTRAP_BASIC
				throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else
				return this;
#endif
			if (newValue == null)
				newValue = Empty;

			return ReplaceUnchecked (oldValue, newValue);
		}

		private unsafe String ReplaceUnchecked (String oldValue, String newValue)
		{
			if (oldValue.Length > Length)
#if BOOTSTRAP_BASIC
				throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else
				return this;
#endif

			if (oldValue.Length == 1 && newValue.Length == 1) {
				return Replace (oldValue[0], newValue[0]);
				// ENHANCE: It would be possible to special case oldValue.Length == newValue.Length
				// because the Length of the result would be this.Length and Length calculation unneccesary
			}

			const int maxValue = 200; // Allocate 800 byte maximum
			int* dat = stackalloc int[maxValue];
			/* FIXME: Avoid ToCharArray. */
			fixed (char* source = ToCharArray (), replace = newValue.ToCharArray ()) {
				int i = 0, count = 0;
				while (i < Length) {
					int found = IndexOfUnchecked (oldValue, i, Length - i);
					if (found < 0)
						break;
					else {
						if (count < maxValue)
							dat[count++] = found;
						else
							return ReplaceFallback (oldValue, newValue, maxValue);
					}
					i = found + oldValue.Length;
				}
				if (count == 0)
#if BOOTSTRAP_BASIC
				throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else
				return this;
#endif
				int nlen = 0;
				checked {
					try {
						nlen = this.Length + ((newValue.Length - oldValue.Length) * count);
					} catch (OverflowException) {
						throw new OutOfMemoryException ();
					}
				}
				/* FIXME: Use ENCODING_ASCII when possible. */
				String tmp = FastAllocateString (nlen, ENCODING_UTF16);

				int curPos = 0, lastReadPos = 0;
				fixed (char* dest = tmp) {
					for (int j = 0; j < count; j++) {
						int precopy = dat[j] - lastReadPos;
						CharCopy (dest + curPos, source + lastReadPos, precopy);
						curPos += precopy;
						lastReadPos = dat[j] + oldValue.Length;
						CharCopy (dest + curPos, replace, newValue.Length);
						curPos += newValue.Length;
					}
					CharCopy (dest + curPos, source + lastReadPos, Length - lastReadPos);
				}
				return tmp;
			}
		}

		private String ReplaceFallback (String oldValue, String newValue, int testedCount)
		{
			int lengthEstimate = this.Length + ((newValue.Length - oldValue.Length) * testedCount);
			StringBuilder sb = new StringBuilder (lengthEstimate);
			for (int i = 0; i < Length;) {
				int found = IndexOfUnchecked (oldValue, i, Length - i);
				if (found < 0) {
					sb.Append (InternalSubString (i, Length - i));
					break;
				}
				sb.Append (InternalSubString (i, found - i));
				sb.Append (newValue);
				i = found + oldValue.Length;
			}
			return sb.ToString ();

		}

		unsafe String PadHelper (int totalWidth, char paddingChar, bool isRightPadded)
		{
			if (totalWidth < 0)
				throw new ArgumentOutOfRangeException ("totalWidth", "Non-negative number required");
			if (totalWidth <= Length)
#if BOOTSTRAP_BASIC
				throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else			
				return this;
#endif
			/* FIXME: Use ENCODING_ASCII when possible. */
			string result = FastAllocateString (totalWidth, ENCODING_UTF16);

			/* FIXME: Avoid ToCharArray. */
			fixed (char* src = ToCharArray ())
			fixed (byte *destByte = result) {
				char* dest = (char*)destByte;
				if (isRightPadded) {
					CharCopy (dest, src, Length);
					char *end = dest + totalWidth;
					char *p = dest + Length;
					while (p < end) {
						*p++ = paddingChar;
					}
				} else {
					char *p = dest;
					char *end = p + totalWidth - Length;
					while (p < end) {
						*p++ = paddingChar;
					}
					CharCopy (p, src, Length);
				}
			}

			return result;
		}

		internal bool StartsWithOrdinalUnchecked (String value)
		{
#if BOOTSTRAP_BASIC
			throw new NotImplementedException ("BOOTSTRAP_BASIC");
#else
			return Length >= value.Length && CompareOrdinalUnchecked (this, 0, value.Length, value, 0, value.Length) == 0;
#endif
		}

		internal unsafe bool IsAscii ()
		{
			if (IsCompact)
				return true;
			fixed (byte* srcByte = &m_firstByte) {
				char* src = (char*)srcByte;
				char* end_ptr = src + Length;
				char* str_ptr = src;

				while (str_ptr != end_ptr) {
					if (*str_ptr >= 0x80)
						return false;

					++str_ptr;
				}
			}

			return true;
		}

		internal bool IsFastSort ()
		{
			return false;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static string InternalIsInterned (string str);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static string InternalIntern (string str);

		internal static unsafe void CharCopy (char *dest, char *src, int count) {
			// Same rules as for memcpy, but with the premise that 
			// chars can only be aligned to even addresses if their
			// enclosing types are correctly aligned
			if ((((int)(byte*)dest | (int)(byte*)src) & 3) != 0) {
				if (((int)(byte*)dest & 2) != 0 && ((int)(byte*)src & 2) != 0 && count > 0) {
					((short*)dest) [0] = ((short*)src) [0];
					dest++;
					src++;
					count--;
				}
				if ((((int)(byte*)dest | (int)(byte*)src) & 2) != 0) {
					Buffer.memcpy2 ((byte*)dest, (byte*)src, count * 2);
					return;
				}
			}
			Buffer.memcpy4 ((byte*)dest, (byte*)src, count * 2);
		}

		#region Runtime method-to-ir dependencies

		/* helpers used by the runtime as well as above or eslewhere in corlib */
		static unsafe void memset (byte *dest, int val, int len)
		{
			if (len < 8) {
				while (len != 0) {
					*dest = (byte)val;
					++dest;
					--len;
				}
				return;
			}
			if (val != 0) {
				val = val | (val << 8);
				val = val | (val << 16);
			}
			// align to 4
			int rest = (int)dest & 3;
			if (rest != 0) {
				rest = 4 - rest;
				len -= rest;
				do {
					*dest = (byte)val;
					++dest;
					--rest;
				} while (rest != 0);
			}
			while (len >= 16) {
				((int*)dest) [0] = val;
				((int*)dest) [1] = val;
				((int*)dest) [2] = val;
				((int*)dest) [3] = val;
				dest += 16;
				len -= 16;
			}
			while (len >= 4) {
				((int*)dest) [0] = val;
				dest += 4;
				len -= 4;
			}
			// tail bytes
			while (len > 0) {
				*dest = (byte)val;
				dest++;
				len--;
			}
		}

		static unsafe void memcpy (byte *dest, byte *src, int size)
		{
			Buffer.Memcpy (dest, src, size);
		}

		/* Used by the runtime */
		internal static unsafe void bzero (byte *dest, int len) {
			memset (dest, 0, len);
		}

		internal static unsafe void bzero_aligned_1 (byte *dest, int len) {
			((byte*)dest) [0] = 0;
		}

		internal static unsafe void bzero_aligned_2 (byte *dest, int len) {
			((short*)dest) [0] = 0;
		}

		internal static unsafe void bzero_aligned_4 (byte *dest, int len) {
			((int*)dest) [0] = 0;
		}

		internal static unsafe void bzero_aligned_8 (byte *dest, int len) {
			((long*)dest) [0] = 0;
		}

		internal static unsafe void memcpy_aligned_1 (byte *dest, byte *src, int size) {
			((byte*)dest) [0] = ((byte*)src) [0];
		}

		internal static unsafe void memcpy_aligned_2 (byte *dest, byte *src, int size) {
			((short*)dest) [0] = ((short*)src) [0];
		}

		internal static unsafe void memcpy_aligned_4 (byte *dest, byte *src, int size) {
			((int*)dest) [0] = ((int*)src) [0];
		}

		internal static unsafe void memcpy_aligned_8 (byte *dest, byte *src, int size) {
			((long*)dest) [0] = ((long*)src) [0];
		}

		#endregion

		// Certain constructors are redirected to CreateString methods with
		// matching argument list. The this pointer should not be used.

		private unsafe String CreateString (sbyte* value)
		{
			if (value == null)
				return Empty;

			byte* bytes = (byte*) value;
			int length = 0;

			try {
				while (bytes++ [0] != 0)
					length++;
			} catch (NullReferenceException) {
				throw new ArgumentOutOfRangeException ("ptr", "Value does not refer to a valid string.");
			}

			return CreateString (value, 0, length, null);
		}

		unsafe String CreateString (sbyte* value, int startIndex, int length)
		{
			return CreateString (value, startIndex, length, null);
		}

		unsafe string CreateString (char *value)
		{
			return CtorCharPtr (value);
		}

		unsafe string CreateString (char *value, int startIndex, int length)
		{
			return CtorCharPtrStartLength (value, startIndex, length);
		}

		string CreateString (char [] val, int startIndex, int length)
		{
			return CtorCharArrayStartLength (val, startIndex, length);
		}

		string CreateString (char [] val)
		{
			return CtorCharArray (val);
		}

		unsafe string CreateString (char c, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count");
			if (count == 0)
				return Empty;
			bool compact = (int)c <= 0x7F;
			string result = FastAllocateString (count, compact ? ENCODING_ASCII : ENCODING_UTF16);
			fixed (byte* destByte = &result.m_firstByte) {
				if (compact) {
					byte* dest = destByte;
					byte* p = dest;
					byte* end = p + count;
					while (p < end) {
						*p = (byte)c;
						p++;
					}
				} else {
					char* dest = (char*)destByte;
					char *p = dest;
					char *end = p + count;
					while (p < end) {
						*p = c;
						p++;
					}
				}
			}
			return result;
		}

		private unsafe String CreateString (sbyte* value, int startIndex, int length, Encoding enc)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", "Non-negative number required.");
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException ("startIndex", "Non-negative number required.");
			if (value + startIndex < value)
				throw new ArgumentOutOfRangeException ("startIndex", "Value, startIndex and length do not refer to a valid string.");

			if (enc == null) {
				if (value == null)
					throw new ArgumentNullException ("value");
				if (length == 0)
					return Empty;

				enc = Encoding.Default;
			}

			byte [] bytes = new byte [length];

			if (length != 0)
				fixed (byte* bytePtr = bytes)
					try {
						if (value == null)
							throw new ArgumentOutOfRangeException ("ptr", "Value, startIndex and length do not refer to a valid string.");
						memcpy (bytePtr, (byte*) (value + startIndex), length);
					} catch (NullReferenceException) {
						throw new ArgumentOutOfRangeException ("ptr", "Value, startIndex and length do not refer to a valid string.");
					}

			// GetString () is called even when length == 0
			return enc.GetString (bytes);
		}

		// Joins an array of strings together as one string with a separator between each original string.
		//
		[System.Security.SecuritySafeCritical]	// auto-generated
		public unsafe static String Join(String separator, String[] value, int startIndex, int count) {
			//Range check the array
			if (value == null)
				throw new ArgumentNullException("value");

			if (startIndex < 0)
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));

			if (startIndex > value.Length - count)
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
			Contract.EndContractBlock();

			//Treat null as empty string.
			if (separator == null) {
				separator = Empty;
			}

			//If count is 0, that skews a whole bunch of the calculations below, so just special case that.
			if (count == 0) {
				return Empty;
			}
			
			int jointLength = 0;
			//Figure out the total length of the strings in value
			int endIndex = startIndex + count - 1;
			for (int stringToJoinIndex = startIndex; stringToJoinIndex <= endIndex; stringToJoinIndex++) {
				if (value[stringToJoinIndex] != null) {
					jointLength += value[stringToJoinIndex].Length;
				}
			}
			
			//Add enough room for the separator.
			jointLength += (count - 1) * separator.Length;

			// Note that we may not catch all overflows with this check (since we could have wrapped around the 4gb range any number of times
			// and landed back in the positive range.) The input array might be modifed from other threads, 
			// so we have to do an overflow check before each append below anyway. Those overflows will get caught down there.
			if ((jointLength < 0) || ((jointLength + 1) < 0) ) {
				throw new OutOfMemoryException();
			}

			//If this is an empty string, just return.
			if (jointLength == 0) {
				return Empty;
			}

			bool compact = separator.IsCompact;
			if (compact) {
				for (int i = 0; i < value.Length; ++i) {
					if (value[i] != null && !value[i].IsCompact) {
						compact = false;
						break;
					}
				}
			}
			string jointString = FastAllocateString(jointLength, compact ? ENCODING_ASCII : ENCODING_UTF16);
			fixed (byte* pointerToJointStringByte = &jointString.m_firstByte) {
				if (compact) {
					byte* dest = pointerToJointStringByte;
					if (value[startIndex] != null) {
						fixed (byte* src = &value[startIndex].m_firstByte)
							memcpy(dest, src, value[startIndex].Length);
						dest += value[startIndex].Length;
					}
					for (int i = startIndex + 1; i <= endIndex; ++i) {
						fixed (byte* src = &separator.m_firstByte)
							memcpy(dest, src, separator.Length);
						dest += separator.Length;
						if (value[i] == null)
							continue;
						fixed (byte* src = &value[i].m_firstByte)
							memcpy(dest, src, value[i].Length);
						dest += value[i].Length;
					}
					Contract.Assert(*dest == '\0', "String must be null-terminated!");
				} else {
					char* pointerToJointString = (char*)pointerToJointStringByte;
					UnSafeCharBuffer charBuffer = new UnSafeCharBuffer( pointerToJointString, jointLength);

					// Append the first string first and then append each following string prefixed by the separator.
					charBuffer.AppendString( value[startIndex] );
					for (int stringToJoinIndex = startIndex + 1; stringToJoinIndex <= endIndex; stringToJoinIndex++) {
						charBuffer.AppendString( separator );
						charBuffer.AppendString( value[stringToJoinIndex] );
					}
					Contract.Assert(*(pointerToJointString + charBuffer.Length) == '\0', "String must be null-terminated!");
				}
			}

			return jointString;
		}

		[System.Security.SecuritySafeCritical]	// auto-generated
		private unsafe static int CompareOrdinalIgnoreCaseHelper(String strA, String strB) {
			Contract.Requires(strA != null);
			Contract.Requires(strB != null);
			Contract.EndContractBlock();
			int length = Math.Min(strA.Length, strB.Length);

			fixed (char* ap = strA, bp = strB)
			{
				if (strA.IsCompact && strB.IsCompact) {
					byte* a = (byte*)ap;
					byte* b = (byte*)bp;
					while (length != 0) {
						int charA = *a;
						int charB = *b;
						if ((uint)(charA - 'a') <= (uint)('z' - 'a')) charA -= 0x20;
						if ((uint)(charB - 'a') <= (uint)('z' - 'a')) charB -= 0x20;
						if (charA != charB)
							return charA - charB;
						a++;
						b++;
						length--;
					}
				} else if (strA.IsCompact) {
					byte* a = (byte*)ap;
					char* b = bp;
					while (length != 0) {
						int charA = *a;
						int charB = *b;
						Contract.Assert(charB <= 0x7F, "strings have to be ASCII");
						if ((uint)(charA - 'a') <= (uint)('z' - 'a')) charA -= 0x20;
						if ((uint)(charB - 'a') <= (uint)('z' - 'a')) charB -= 0x20;
						if ((char)charA != charB)
							return charA - charB;
						a++;
						b++;
						length--;
					}
				} else if (strB.IsCompact) {
					char* a = ap;
					byte* b = (byte*)bp;
					while (length != 0) {
						int charA = *a;
						int charB = *b;
						Contract.Assert(charA <= 0x7F, "strings have to be ASCII");
						if ((uint)(charA - 'a') <= (uint)('z' - 'a')) charA -= 0x20;
						if ((uint)(charB - 'a') <= (uint)('z' - 'a')) charB -= 0x20;
						if (charA != charB)
							return charA - charB;
						a++;
						b++;
						length--;
					}
				} else {
					char* a = ap;
					char* b = bp;
					while (length != 0) {
						int charA = *a;
						int charB = *b;
						Contract.Assert((charA | charB) <= 0x7F, "strings have to be ASCII");
						if ((uint)(charA - 'a') <= (uint)('z' - 'a')) charA -= 0x20;
						if ((uint)(charB - 'a') <= (uint)('z' - 'a')) charB -= 0x20;
						if (charA != charB)
							return charA - charB;
						a++;
						b++;
						length--;
					}
				}
				return strA.Length - strB.Length;
			}
		}

		//
		// This is a helper method for the security team.  They need to uppercase some strings (guaranteed to be less 
		// than 0x80) before security is fully initialized.	 Without security initialized, we can't grab resources (the nlp's)
		// from the assembly.  This provides a workaround for that problem and should NOT be used anywhere else.
		//
		[System.Security.SecuritySafeCritical]	// auto-generated
		internal unsafe static string SmallCharToUpper(string strIn) {
			Contract.Requires(strIn != null);
			Contract.EndContractBlock();
			//
			// Get the length and pointers to each of the buffers.	Walk the length
			// of the string and copy the characters from the inBuffer to the outBuffer,
			// capitalizing it if necessary.  We assert that all of our characters are
			// less than 0x80.
			//
			int length = strIn.Length;
			String strOut = FastAllocateString(length, ENCODING_ASCII);
			fixed (char* inPtr = strIn)
				fixed (byte* outPtr = &strOut.m_firstByte) {
				if (strIn.IsCompact) {
					for (int i = 0; i < length; ++i) {
						int c = (char)((byte*)inPtr)[i];
						Contract.Assert(c <= 0x7F, "string has to be ASCII");
						// uppercase - notice that we need just one compare
						if ((uint)(c - 'a') <= (uint)('z' - 'a')) c -= 0x20;
						outPtr[i] = (byte)c;
					}
				} else {
					for(int i = 0; i < length; i++) {
						int c = inPtr[i];
						Contract.Assert(c <= 0x7F, "string has to be ASCII");
						// uppercase - notice that we need just one compare
						if ((uint)(c - 'a') <= (uint)('z' - 'a')) c -= 0x20;
						((byte*)outPtr)[i] = (byte)c;
					}
				}
				Contract.Assert(((byte*)outPtr)[length]=='\0', "((byte*)outPtr)[length]=='\0'");
			}
			return strOut;
		}

		[System.Security.SecuritySafeCritical]	// auto-generated
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		private unsafe static bool EqualsHelper(String strA, String strB) {
			Contract.Requires(strA != null);
			Contract.Requires(strB != null);
			Contract.Requires(strA.Length == strB.Length);

			int length = strA.Length;

			fixed (char* ap = strA, bp = strB) {
				if (strA.IsCompact && strB.IsCompact) {
					byte* a = (byte*)ap;
					byte* b = (byte*)bp;
					/* FIXME: Unroll. */
					for (int i = 0; i < strA.Length; ++i)
						if (a[i] != b[i])
							return false;
					return true;
				} else if (strA.IsCompact) {
					byte* a = (byte*)ap;
					char* b = bp;
					/* FIXME: Unroll. */
					for (int i = 0; i < strA.Length; ++i)
						if ((char)a[i] != b[i])
							return false;
					return true;
				} else if (strB.IsCompact) {
					char* a = ap;
					byte* b = (byte*)bp;
					/* FIXME: Unroll. */
					for (int i = 0; i < strA.Length; ++i)
						if (a[i] != (char)b[i])
							return false;
					return true;
				} else {
					char* a = ap;
					char* b = bp;
					// unroll the loop
#if AMD64
					// for AMD64 bit platform we unroll by 12 and
					// check 3 qword at a time. This is less code
					// than the 32 bit case and is shorter
					// pathlength
					while (length >= 12)
					{
						if (*(long*)a	  != *(long*)b) return false;
						if (*(long*)(a+4) != *(long*)(b+4)) return false;
						if (*(long*)(a+8) != *(long*)(b+8)) return false;
						a += 12; b += 12; length -= 12;
					}
#else
					while (length >= 10)
					{
						if (*(int*)a != *(int*)b) return false;
						if (*(int*)(a+2) != *(int*)(b+2)) return false;
						if (*(int*)(a+4) != *(int*)(b+4)) return false;
						if (*(int*)(a+6) != *(int*)(b+6)) return false;
						if (*(int*)(a+8) != *(int*)(b+8)) return false;
						a += 10; b += 10; length -= 10;
					}
#endif
					// This depends on the fact that the String objects are
					// always zero terminated and that the terminating zero is not included
					// in the length. For odd string sizes, the last compare will include
					// the zero terminator.
					while (length > 0)
					{
						if (*(int*)a != *(int*)b) break;
						a += 2; b += 2; length -= 2;
					}
					return (length <= 0);
				}
			}
		}

		[System.Security.SecuritySafeCritical]	// auto-generated
		private unsafe static int CompareOrdinalHelper(String strA, String strB) {
			Contract.Requires(strA != null);
			Contract.Requires(strB != null);

			int length = Math.Min(strA.Length, strB.Length);
			int diffOffset = -1;

			/* FIXME: Avoid ToCharArray. */
			fixed (char* ap = strA.ToCharArray ()) fixed (char* bp = strB.ToCharArray ())
			{
				char* a = ap;
				char* b = bp;

				// unroll the loop
				while (length >= 10)
				{
					if (*(int*)a != *(int*)b) { 
						diffOffset = 0; 
						break;
					}
					
					if (*(int*)(a+2) != *(int*)(b+2)) {
						diffOffset = 2;
						break;
					}
					
					if (*(int*)(a+4) != *(int*)(b+4)) {
						diffOffset = 4;
						break;
					}
					
					if (*(int*)(a+6) != *(int*)(b+6)) {
						diffOffset = 6;
						break;
					}
					
					if (*(int*)(a+8) != *(int*)(b+8)) {
						diffOffset = 8;
						break;
					}
					a += 10; 
					b += 10; 
					length -= 10;
				}

				if( diffOffset != -1) {
					// we already see a difference in the unrolled loop above
					a += diffOffset;
					b += diffOffset;
					int order;
					if ( (order = (int)*a - (int)*b) != 0) {
						return order;
					}
					Contract.Assert( *(a+1) != *(b+1), "This byte must be different if we reach here!");
					return ((int)*(a+1) - (int)*(b+1));					   
				}

				// now go back to slower code path and do comparison on 4 bytes one time.
				// Following code also take advantage of the fact strings will 
				// use even numbers of characters (runtime will have a extra zero at the end.)
				// so even if length is 1 here, we can still do the comparsion.	 
				while (length > 0) {
					if (*(int*)a != *(int*)b) {
						break;
					}
					a += 2; 
					b += 2; 
					length -= 2;
				}

				if( length > 0) { 
					int c;
					// found a different int on above loop
					if ( (c = (int)*a - (int)*b) != 0) {
						return c;
					}
					Contract.Assert( *(a+1) != *(b+1), "This byte must be different if we reach here!");
					return ((int)*(a+1) - (int)*(b+1));										   
				}

				// At this point, we have compared all the characters in at least one string.
				// The longer string will be larger.
				return strA.Length - strB.Length;
			}
		}

		// Converts a substring of this string to an array of characters.  Copies the
		// characters of this string beginning at position startIndex and ending at
		// startIndex + length - 1 to the character array buffer, beginning
		// at bufferStartIndex.
		//
		[System.Security.SecuritySafeCritical]	// auto-generated
		unsafe public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) {
			if (destination == null)
				throw new ArgumentNullException("destination");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
			if (sourceIndex < 0)
				throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			if (count > Length - sourceIndex)
				throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
			if (destinationIndex > destination.Length - count || destinationIndex < 0)
				throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
			Contract.EndContractBlock();

			// Note: fixed does not like empty arrays
			if (count > 0)
			{
				fixed (byte* src = &m_firstByte) {
					if (IsCompact) {
						/* FIXME: Unroll. */
						for (int i = 0; i < count; ++i)
							destination[destinationIndex + i] = (char)src[sourceIndex + i];
					} else {
						fixed (char* dest = destination)
							wstrcpy(dest + destinationIndex, (char*)src + sourceIndex, count);
					}
				}
			}
		}

		// Returns the entire string as an array of characters.
		[System.Security.SecuritySafeCritical]	// auto-generated
		unsafe public char[] ToCharArray() {
			// <STRIP> huge performance improvement for short strings by doing this </STRIP>
			int length = Length;
			char[] chars = new char[length];
			if (length > 0) {
				fixed (byte* src = &m_firstByte)
				fixed (char* dest = chars) {
					if (IsCompact) {
						/* FIXME: Unroll. */
						for (int i = 0; i < length; ++i)
							dest[i] = (char)src[i];
					} else {
						wstrcpy(dest, (char*)src, length);
					}
				}
			}
			return chars;
		}

		// Returns a substring of this string as an array of characters.
		//
		[System.Security.SecuritySafeCritical]	// auto-generated
		unsafe public char[] ToCharArray(int startIndex, int length)
		{
			// Range check everything.
			if (startIndex < 0 || startIndex > Length || startIndex > Length - length)
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			if (length < 0)
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			Contract.EndContractBlock();

			char[] chars = new char[length];
			if(length > 0) {
				fixed (byte* src = &m_firstByte)
				fixed (char* dest = chars) {
					if (IsCompact) {
						/* FIXME: Unroll. */
						for (int i = 0; i < length; ++i)
							dest[i] = (char)src[startIndex + i];
					} else {
						wstrcpy(dest, (char*)src + startIndex, length);
					}
				}
			}
			return chars;
		}

		// Use this if and only if you need the hashcode to not change across app domains (e.g. you have an app domain agile
		// hash table).
		[System.Security.SecuritySafeCritical]	// auto-generated
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal int GetLegacyNonRandomizedHashCode() {
			unsafe {
				/* FIXME: Avoid ToCharArray. */
				fixed (char *src = this.ToCharArray()) {
					Contract.Assert(src[this.Length] == '\0', "src[this.Length] == '\\0'");
					Contract.Assert( ((int)src)%4 == 0, "Managed string should start at 4 bytes boundary");

#if WIN32
					int hash1 = (5381<<16) + 5381;
#else
					int hash1 = 5381;
#endif
					int hash2 = hash1;

#if WIN32
					// 32 bit machines.
					int* pint = (int *)src;
					int len = this.Length;
					while (len > 2)
					{
						hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
						hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ pint[1];
						pint += 2;
						len	 -= 4;
					}

					if (len > 0)
					{
						hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
					}
#else
					int		c;
					char *s = src;
					while ((c = s[0]) != 0) {
						hash1 = ((hash1 << 5) + hash1) ^ c;
						c = s[1];
						if (c == 0)
							break;
						hash2 = ((hash2 << 5) + hash2) ^ c;
						s += 2;
					}
#endif
#if !MONO && DEBUG
					// We want to ensure we can change our hash function daily.
					// This is perfectly fine as long as you don't persist the
					// value from GetHashCode to disk or count on String A 
					// hashing before string B.	 Those are bugs in your code.
					hash1 ^= ThisAssembly.DailyBuildNumber;
#endif
					return hash1 + (hash2 * 1566083941);
				}
			}
		}

		[System.Security.SecurityCritical]	// auto-generated
		unsafe string InternalSubString(int startIndex, int length) {
			Contract.Assert( startIndex >= 0 && startIndex <= this.Length, "StartIndex is out of range!");
			Contract.Assert( length >= 0 && startIndex <= this.Length - length, "length is out of range!");			   
			String result = FastAllocateString(length, IsCompact ? ENCODING_ASCII : ENCODING_UTF16);
			fixed (char* dest = result)
			fixed (byte* src = &m_firstByte) {
				if (IsCompact)
					memcpy ((byte*)dest, src + startIndex, length);
				else
					CharCopy (dest, (char*)src + startIndex, length);
			}
			return result;
		}

		// Helper for encodings so they can talk to our buffer directly
		// stringLength must be the exact size we'll expect
		[System.Security.SecurityCritical]	// auto-generated
		unsafe static internal String CreateStringFromEncoding(
			byte* bytes, int byteLength, Encoding encoding) {
			Contract.Requires(bytes != null);
			Contract.Requires(byteLength >= 0);

			// Get our string length
			int stringLength = encoding.GetCharCount(bytes, byteLength, null);
			Contract.Assert(stringLength >= 0, "stringLength >= 0");

			// They gave us an empty string if they needed one
			// 0 bytelength might be possible if there's something in an encoder
			if (stringLength == 0)
				return Empty;

			/* FIXME: This could use ENCODING_ASCII, with a bit more cleverness. */
			String s = FastAllocateString(stringLength, ENCODING_UTF16);
			fixed (char* pTempChars = s)
			{
				int doubleCheck = encoding.GetChars(bytes, byteLength, pTempChars, stringLength, null);
				Contract.Assert(
					stringLength == doubleCheck,
					"Expected encoding.GetChars to return same length as encoding.GetCharCount");
			}

			return s;
		}

		[System.Security.SecurityCritical]	// auto-generated
		[ResourceExposure(ResourceScope.None)]
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		internal extern static String FastAllocateString(int length, int encoding);

		[System.Security.SecuritySafeCritical]	// auto-generated
		unsafe private static void FillNoncompactStringChecked(String dest, int destPos, String src) {
			Contract.Requires(dest != null);
			Contract.Requires(src != null);
			if (src.Length > dest.Length - destPos) {
				throw new IndexOutOfRangeException();
			}
			Contract.EndContractBlock();

			fixed (char *pSrc = src)
			fixed (char *pDest = dest) {
				if (src.IsCompact) {
					for (int i = 0; i < src.Length; ++i)
						pDest[destPos + i] = (char)((byte*)pSrc)[i];
				} else {
					wstrcpy(pDest + destPos, (char*)pSrc, src.Length);
				}
			}
		}

		[System.Security.SecuritySafeCritical]	// auto-generated
		unsafe private static void FillCompactStringChecked(String dest, int destPos, String src) {
			Contract.Requires(dest != null);
			Contract.Requires(src != null);
			if (src.Length > dest.Length - destPos) {
				throw new IndexOutOfRangeException();
			}
			Contract.EndContractBlock();

			Contract.Assert(src.IsCompact, "Cannot fill compact string from non-compact string.");
			fixed (byte *pSrc = &src.m_firstByte)
			fixed (byte *pDest = &dest.m_firstByte) {
				memcpy(pDest + destPos, pSrc, src.Length);
			}
		}

		[System.Security.SecuritySafeCritical]	// auto-generated
		private String CtorCharArray(char [] value)
		{
			if (value == null || value.Length == 0)
				return Empty;
			bool compact = CompactRepresentable(value);
			String result = FastAllocateString(value.Length, SelectEncoding(compact));
			unsafe {
				fixed (char* dest = result)
				fixed (char* source = value) {
					if (compact) {
						for (int i = 0; i < value.Length; ++i)
							((byte*)dest)[i] = (byte)source[i];
					} else {
						wstrcpy(dest, source, value.Length);
					}
				}
			}
			return result;
		}

		[System.Security.SecuritySafeCritical]	// auto-generated
		private String CtorCharArrayStartLength(char [] value, int startIndex, int length)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			if (startIndex < 0)
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));

			if (length < 0)
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));

			if (startIndex > value.Length - length)
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			Contract.EndContractBlock();

			if (length <= 0)
				return Empty;

			/* FIXME: Could infer non-compact encoding even if result could be compact. */
			bool compact = CompactRepresentable(value);
			String result = FastAllocateString(length, SelectEncoding(compact));
			unsafe {
				fixed (byte* destByte = result)
				fixed (char* source = value) {
					if (compact) {
						for (int i = 0; i < length; ++i)
							destByte[i] = (byte)source[startIndex + i];
					} else {
						wstrcpy((char*)destByte, source + startIndex, length);
					}
				}
			}
			return result;
		}

		[System.Security.SecuritySafeCritical]	// auto-generated
		private String CtorCharCount(char c, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "count"));
			if (count == 0)
				return Empty;

			bool compact = CompactRepresentable(c);
			String result = FastAllocateString(count, SelectEncoding(compact));
			unsafe {
				fixed (char* dest = result) {
					if (compact) {
						/* FIXME: Unroll. */
						for (int i = 0; i < count; ++i)
							((byte*)dest)[i] = (byte)c;
					} else {
						char *dmem = dest;
						while (((uint)dmem & 3) != 0 && count > 0) {
							*dmem++ = c;
							count--;
						}
						uint cc = (uint)((c << 16) | c);
						if (count >= 4) {
							count -= 4;
							do{
								((uint *)dmem)[0] = cc;
								((uint *)dmem)[1] = cc;
								dmem += 4;
								count -= 4;
							} while (count >= 0);
						}
						if ((count & 2) != 0) {
							((uint *)dmem)[0] = cc;
							dmem += 2;
						}
						if ((count & 1) != 0)
							dmem[0] = c;
					}
				}
			}
			return result;
		}

		[System.Security.SecurityCritical]	// auto-generated
		private unsafe String CtorCharPtr(char *ptr)
		{
			if (ptr == null)
				return Empty;

#if !FEATURE_PAL
			if (ptr < (char*)64000)
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeStringPtrNotAtom"));
#endif // FEATURE_PAL

			Contract.Assert(this == null, "this == null");		  // this is the string constructor, we allocate it

			try {
				int count = wcslen(ptr);
				if (count == 0)
					return Empty;
				bool compact = CompactRepresentable(ptr, count);

				String result = FastAllocateString(count, SelectEncoding(compact));
				fixed (char* dest = result) {
					if (compact) {
						for (int i = 0; i < count; ++i)
							((byte*)dest)[i] = (byte)ptr[i];
					} else {
						wstrcpy(dest, ptr, count);
					}
				}
				return result;
			}
			catch (NullReferenceException) {
				throw new ArgumentOutOfRangeException("ptr", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
			}
		}
		[System.Security.SecurityCritical]	// auto-generated
		#if !FEATURE_CORECLR
		[System.Runtime.ForceTokenStabilization]
		#endif //!FEATURE_CORECLR
		private unsafe String CtorCharPtrStartLength(char *ptr, int startIndex, int length)
		{
			if (length < 0) {
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
			}

			if (startIndex < 0) {
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
			}
			Contract.EndContractBlock();
			Contract.Assert(this == null, "this == null");		  // this is the string constructor, we allocate it

			char *pFrom = ptr + startIndex;
			if (pFrom < ptr) {
				// This means that the pointer operation has had an overflow
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
			}

			if (length == 0)
				return Empty;

			bool compact = CompactRepresentable(ptr + startIndex, length);
			String result = FastAllocateString(length, SelectEncoding(compact));

			try {
				fixed(char* dest = result) {
					if (compact) {
						for (int i = 0; i < length; ++i)
							((byte*)dest)[i] = (byte)ptr[startIndex + i];
					} else {
						wstrcpy(dest, pFrom, length);
					}
				}
				return result;
			}
			catch (NullReferenceException) {
				throw new ArgumentOutOfRangeException("ptr", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
			}
		}
		[System.Security.SecuritySafeCritical]	// auto-generated
		public String Insert(int startIndex, String value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (startIndex < 0 || startIndex > this.Length)
				throw new ArgumentOutOfRangeException("startIndex");
			Contract.Ensures(Contract.Result<String>() != null);
			Contract.Ensures(Contract.Result<String>().Length == this.Length + value.Length);
			Contract.EndContractBlock();
			int oldLength = Length;
			int insertLength = value.Length;
			// In case this computation overflows, newLength will be negative and FastAllocateString throws OutOfMemoryException
			int newLength = oldLength + insertLength;
			if (newLength == 0)
				return Empty;
			bool compact = IsCompact && value.IsCompact;
			String result = FastAllocateString(newLength, SelectEncoding(compact));
			unsafe
			{
				fixed (byte* srcThis = &m_firstByte)
				fixed (char* srcInsert = value)
				fixed (char* dst = result) {
					if (compact) {
						memcpy((byte*)dst, srcThis, startIndex);
						memcpy((byte*)dst + startIndex, (byte*)srcInsert, insertLength);
						memcpy((byte*)dst + startIndex + insertLength, srcThis + startIndex, oldLength - startIndex);
					} else {
						wstrcpy(dst, (char*)srcThis, startIndex);
						wstrcpy(dst + startIndex, srcInsert, insertLength);
						wstrcpy(dst + startIndex + insertLength, (char*)srcThis + startIndex, oldLength - startIndex);
					}
				}
			}
			return result;
		}
		[System.Security.SecuritySafeCritical]	// auto-generated
		public String Remove(int startIndex, int count)
		{
			if (startIndex < 0)
				throw new ArgumentOutOfRangeException("startIndex", 
					Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", 
					Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
			if (count > Length - startIndex)
				throw new ArgumentOutOfRangeException("count", 
					Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
			Contract.Ensures(Contract.Result<String>() != null);
			Contract.Ensures(Contract.Result<String>().Length == this.Length - count);
			Contract.EndContractBlock();
			int newLength = Length - count;
			if (newLength == 0)
				return Empty;
			bool compact = IsCompact;
			String result = FastAllocateString(newLength, SelectEncoding(compact));
			unsafe
			{
				fixed (byte* src = &m_firstByte)
				fixed (char* dst = result) {
					if (compact) {
						memcpy((byte*)dst, src, startIndex);
						memcpy((byte*)dst + startIndex, src + startIndex + count, newLength - startIndex);
					} else {
						wstrcpy(dst, (char*)src, startIndex);
						wstrcpy(dst + startIndex, (char*)src + startIndex + count, newLength - startIndex);
					}
				}
			}
			return result;
		}
	
		[System.Security.SecuritySafeCritical]	// auto-generated
		unsafe public static String Copy (String str) {
			if (str==null) {
				throw new ArgumentNullException("str");
			}
			Contract.Ensures(Contract.Result<String>() != null);
			Contract.EndContractBlock();

			int length = str.Length;

			bool compact = str.IsCompact;
			String result = FastAllocateString(length, SelectEncoding(compact));

			fixed (char* src = str)
			fixed (char* dest = result) {
				if (compact)
					memcpy((byte*)dest, (byte*)src, length);
				else
					wstrcpy(dest, src, length);
			}
			return result;
		}
		[System.Security.SecuritySafeCritical]	// auto-generated
		public static String Concat(String str0, String str1) {
			Contract.Ensures(Contract.Result<String>() != null);
			Contract.Ensures(Contract.Result<String>().Length ==
				(str0 == null ? 0 : str0.Length) +
				(str1 == null ? 0 : str1.Length));
			Contract.EndContractBlock();

			if (IsNullOrEmpty(str0)) {
				if (IsNullOrEmpty(str1)) {
					return Empty;
				}
				return str1;
			}

			if (IsNullOrEmpty(str1)) {
				return str0;
			}

			int str0Length = str0.Length;

			bool compact = str0.IsCompact && str1.IsCompact;
			String result = FastAllocateString(str0Length + str1.Length, SelectEncoding(compact));

			if (compact) {
				FillCompactStringChecked(result, 0, str0);
				FillCompactStringChecked(result, str0Length, str1);
			} else {
				FillNoncompactStringChecked(result, 0, str0);
				FillNoncompactStringChecked(result, str0Length, str1);
			}

			return result;
		}
		[System.Security.SecuritySafeCritical]	// auto-generated
		public static String Concat(String str0, String str1, String str2) {
			Contract.Ensures(Contract.Result<String>() != null);
			Contract.Ensures(Contract.Result<String>().Length ==
				(str0 == null ? 0 : str0.Length) +
				(str1 == null ? 0 : str1.Length) +
				(str2 == null ? 0 : str2.Length));
			Contract.EndContractBlock();

			if (str0==null && str1==null && str2==null) {
				return Empty;
			}

			if (str0==null) {
				str0 = Empty;
			}

			if (str1==null) {
				str1 = Empty;
			}

			if (str2 == null) {
				str2 = Empty;
			}

			int totalLength = str0.Length + str1.Length + str2.Length;

			bool compact = str0.IsCompact && str1.IsCompact && str2.IsCompact;
			String result = FastAllocateString(totalLength, SelectEncoding(compact));
			if (compact) {
				FillCompactStringChecked(result, 0, str0);
				FillCompactStringChecked(result, str0.Length, str1);
				FillCompactStringChecked(result, str0.Length + str1.Length, str2);
			} else {
				FillNoncompactStringChecked(result, 0, str0);
				FillNoncompactStringChecked(result, str0.Length, str1);
				FillNoncompactStringChecked(result, str0.Length + str1.Length, str2);
			}

			return result;
		}
		[System.Security.SecuritySafeCritical]	// auto-generated
		public static String Concat(String str0, String str1, String str2, String str3) {
			Contract.Ensures(Contract.Result<String>() != null);
			Contract.Ensures(Contract.Result<String>().Length == 
				(str0 == null ? 0 : str0.Length) +
				(str1 == null ? 0 : str1.Length) +
				(str2 == null ? 0 : str2.Length) +
				(str3 == null ? 0 : str3.Length));
			Contract.EndContractBlock();

			if (str0==null && str1==null && str2==null && str3==null) {
				return Empty;
			}

			if (str0==null) {
				str0 = Empty;
			}

			if (str1==null) {
				str1 = Empty;
			}

			if (str2 == null) {
				str2 = Empty;
			}
			
			if (str3 == null) {
				str3 = Empty;
			}

			int totalLength = str0.Length + str1.Length + str2.Length + str3.Length;

			bool compact = str0.IsCompact && str1.IsCompact && str2.IsCompact && str3.IsCompact;
			String result = FastAllocateString(totalLength, SelectEncoding(compact));

			if (compact) {
				FillCompactStringChecked(result, 0, str0);
				FillCompactStringChecked(result, str0.Length, str1);
				FillCompactStringChecked(result, str0.Length + str1.Length, str2);
				FillCompactStringChecked(result, str0.Length + str1.Length + str2.Length, str3);
			} else {
				FillNoncompactStringChecked(result, 0, str0);
				FillNoncompactStringChecked(result, str0.Length, str1);
				FillNoncompactStringChecked(result, str0.Length + str1.Length, str2);
				FillNoncompactStringChecked(result, str0.Length + str1.Length + str2.Length, str3);
			}

			return result;
		}

		[System.Security.SecuritySafeCritical]	// auto-generated
		private static String ConcatArray(String[] values, int totalLength) {
			bool compact = true;
			for (int i = 0; i < values.Length; ++i) {
				if (!values[i].IsCompact) {
					compact = false;
					break;
				}
			}
			String result = FastAllocateString(totalLength, SelectEncoding(compact));
			int currPos=0;

			if (compact) {
				for (int i = 0; i < values.Length; ++i) {
					Contract.Assert(
						(currPos <= totalLength - values[i].Length),
						"[String.ConcatArray](currPos <= totalLength - values[i].Length)");
					FillCompactStringChecked(result, currPos, values[i]);
					currPos+=values[i].Length;
				}
			} else {
				for (int i=0; i<values.Length; i++) {
					Contract.Assert(
						(currPos <= totalLength - values[i].Length),
						"[String.ConcatArray](currPos <= totalLength - values[i].Length)");
					FillNoncompactStringChecked(result, currPos, values[i]);
					currPos+=values[i].Length;
				}
			}

			return result;
		}

		// Copies the source String (byte buffer) to the destination IntPtr memory allocated with len bytes.
		[System.Security.SecurityCritical]	// auto-generated
		#if !FEATURE_CORECLR
		[System.Runtime.ForceTokenStabilization]
		#endif //!FEATURE_CORECLR
		internal unsafe static void InternalCopy(String src, IntPtr dest,int len)
		{
			if (len == 0)
				return;
			fixed (char* charPtr = src) {
				byte* srcPtr = (byte*)charPtr;
				if (src.IsCompact) {
					char* dstPtr = (char*)dest;
					/* Maintaining the UTF-16 illusion, to be on the safe side. */
					for (int i = 0; i < len; ++i)
						*dstPtr++ = (char)*srcPtr;
				} else {
					byte* dstPtr = (byte*)dest;
					memcpy(dstPtr, srcPtr, len);
				}
			}
		}

	}
}
