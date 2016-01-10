using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Decoding.Bits
{
    public class BitSetBuffer: Common.SafeBitArray
{
    private static readonly long serialVersionUID = 1L;

    /**
     * Logical (ie constructed) size of this bitset, despite the actual size of
     * the super bitset that this class is based on
     */
    private int mSize = 0;
    
    /**
     * Pointer to the next fill index location, when adding bits to this bitset
     * one at a time.
     */
    private int mPointer = 0;

    /**
     * Bitset that buffers bits added one at a time, up to the size of the this
     * bitset. 
     * 
     * Note: the super class bitset behind this class may have a size larger 
     * that the size parameter specified.
     * @param size
     */
    public BitSetBuffer( int size ):base(size)
    {
        mSize = size;
    }
    
    /**
     * Constructs a bitset buffer and preloads it with the bits contained in
     * the bitsToPreload parameter.  If the bitsToPreload are longer than the
     * size of the bitset, only those bits that fit will be preloaded
     * 
     * @param size
     * @param bitsToPreload
     */
    public BitSetBuffer( int size, bool[] bitsToPreload )
    {
        this.SetLength( size );
        
        int pointer = 0;
        
        while( !this.isFull() && pointer < bitsToPreload.Length )
        {
            try
            {
                this.add( bitsToPreload[ pointer ] );
            }
            catch( BitSetFullException e )
            {
                e.printStackTrace();
            }
            
            pointer++;
        }
    }

    /**
     * Constructs a new BitSetBuffer from an existing one
     */
    private BitSetBuffer( BitSetBuffer toCopyFrom )
    {
        this.SetLength( toCopyFrom.size() );
        this.or( toCopyFrom );
        this.mPointer = toCopyFrom.pointer();
    }
    
    /**
     * Current pointer index
     */
    public int pointer()
    {
        return mPointer;
    }

    /**
     * Static method to construct a new BitSetBuffer, preloaded with the bits
     * from the preload parameter, and then filled with the bits from the 
     * second bitsetbuffer parameter.
     * 
     * @param preloadBits - boolean array of bits to be prepended to the new
     *          bitset
     * @param bitsetToAppend - full bitset to be appended to the residual bits array 
     * @return - new Bitset preloaded with residual bits and new bitset
     */
    public static BitSetBuffer merge( bool[] preloadBits, BitSetBuffer bitsetToAppend )
    {
        BitSetBuffer returnValue = new BitSetBuffer( preloadBits.Length + bitsetToAppend.size(), preloadBits );

        int pointer = 0;
        
        while( pointer < bitsetToAppend.size() && !returnValue.isFull() )
        {
            try
            {
                returnValue.add( bitsetToAppend[pointer] );
            }
            catch( BitSetFullException e )
            {
                //e.printStackTrace();
            }
            
            pointer++;
        }
        
        return returnValue;
    }
    
    /**
     * Returns a (new) copy of this bitsetbuffer
     * @return
     */
    public BitSetBuffer copy()
    {
        return new BitSetBuffer( this );
    }
    
    public bool isFull()
    {
        return mPointer >= mSize;
    }

    /**
     * Overrides the in-build size() method of the bitset and returns the value
     * specified at instantiation.  The actual bitset size may be larger than
     * this value, and that size is managed by the super class.
     */
    public int size()
    {
        return mSize;
    }

    /**
     * Clears (sets to false or 0) the bits in this bitset and resets the
     * pointer to zero.
     */
    public void clear()
    {
        this.ClearRange( 0,  mSize );
        mPointer = 0;
    }

    /**
     * Adds a the bit parameters to this bitset, placing it in the index 
     * specified by mPointer, and incrementing mPointer to prepare for the next
     * call to this method
     * @param value
     * @throws BitSetFullException - if the size specified at construction is
     * exceeded.  Invoke full() to determine if the bitset is full either before
     * adding a new bit, or after adding a bit.
     */
    public void add( bool value )
    {
        if( !isFull() )
        {
            this[mPointer++] = value;
        }
        else
        {
            throw new BitSetFullException( "bitset is full -- contains " + ( mPointer + 1 ) + " bits" );
        }
    }
    
    public String toString()
    {
        StringBuilder sb = new StringBuilder();
        
        for( int x = 0; x < mSize; x++ )
        {
            sb.Append( this[x] ? "1" : "0" );
        }
        
        return sb.ToString();
    }
    
    /**
     * Returns a boolean array from startIndex to end of the bitset
     */
    public bool[] getBits( int startIndex )
    {
        return getBits( startIndex, mSize - 1 );
    }
    
    /**
     * Returns a boolean array of the right-most bitCount number of bits
     */
    public bool[] right( int bitCount )
    {
        return getBits( mSize - bitCount - 1 );
    }
    
    /**
     * Returns a boolean array representing the bits located from startIndex
     * through endIndex
     */
    public bool[] getBits( int startIndex, int endIndex )
    {
        bool[] returnValue = null;
        
        if( startIndex >= 0 && 
            startIndex < endIndex && 
            endIndex < mSize )
        {
            returnValue = new bool[ endIndex - startIndex + 1 ];

            int bitsetPointer = startIndex;
            int returnPointer = 0;
            
            while( bitsetPointer <= endIndex )
            {
                returnValue[ returnPointer ] = this[bitsetPointer];
                bitsetPointer++;
                returnPointer++;
            }
        }

        return returnValue;
    }
    
    /**
     * Returns the integer value represented by the bit array
     * @param bits - an array of bit positions that will be treated as if they
     * 			were contiguous bits, with index 0 being the MSB and index
     * 			length - 1 being the LSB
     * @return - integer value of the bit array
     */
    public int getInt( int[] bits )
    {
    	if( bits.Length > 31 )
    	{
    		throw new ArgumentOutOfRangeException( "BitSetBuffer getInt() array length must be less than 32 or the value exceeds integer size" );
    	}
    	
    	int retVal = 0;
    	
    	for( int x = 0; x < bits.Length; x++ )
    	{
    		if( this[bits[ x ]] )
    		{
    			retVal += 1<<( bits.Length - 1 - x );
    		}
    	}
    	
    	return retVal;
    }
    
    /**
     * Returns the integer value represented by the bit array
     * @param bits - an array of bit positions that will be treated as if they
     * 			were contiguous bits, with index 0 being the MSB and index
     * 			length - 1 being the LSB
     * @return - integer value of the bit array
     */
    public long getLong( int[] bits )
    {
    	if( bits.Length > 63 )
    	{
    		throw new ArgumentOutOfRangeException( "BitSetBuffer getLong() array length must be less than 64 or the value exceeds a primitive long size" );
    	}

    	long retVal = 0;
    	
    	for( int x = 0; x < bits.Length; x++ )
    	{
    		if( this[ bits[ x ]] )
    		{
    			retVal += 1<<( bits.Length - 1 - x );
    		}
    	}
    	
    	return retVal;
    }

    /**
     * Converts up to 63 bits from the bit array into an integer and then 
     * formats the value into hexadecimal, prefixing the value with zeros to
     * provide a total length of digitDisplayCount;
     * 
     * @param bits
     * @param digitDisplayCount
     * @return
     */
    public String getHex( int[] bits, int digitDisplayCount )
    {
    	if( bits.Length <= 31 )
    	{
        	int value = getInt( bits );
        	
        	return String.Format( "{0:x0" + digitDisplayCount + "}", value );
    	}
    	else if( bits.Length <= 63 )
    	{
    		long value = getLong( bits );
        	
        	return String.Format( "{0:x0" + digitDisplayCount + "}", value );
    	}
    	else
    	{
    		throw new ArgumentOutOfRangeException( "BitSetBuffer.getHex() maximum array length is 63 bits" );
    	}
    }

    public String getHex( int msb, int lsb, int digitDisplayCount )
    {
    	int length = lsb - msb;
    	
    	if( length <= 31 )
    	{
        	int value = getInt( msb, lsb );
        	
        	return String.Format( "%0" + digitDisplayCount + "X", value );
    	}
    	else if( length <= 63 )
    	{
    		long value = getLong( msb, lsb );
        	
        	return String.Format( "%0" + digitDisplayCount + "X", value );
    	}
    	else
    	{
    		throw new ArgumentOutOfRangeException( "BitSetBuffer.getHex() maximum array length is 63 bits" );
    	}
    }

    /**
     * Returns the integer value represented by the bit range
     * @param start - MSB of the integer
     * @param end - LSB of the integer
     * @return - integer value of the bit range
     */
    public int getInt( int start, int end )
    {
    	if( end - start > 32 )
    	{
    		throw new ArgumentOutOfRangeException( "BitSetBuffer - requested bit length [" + ( end - start ) + "] is larger than an integer (32 bits)" );
    	}
    	
    	int retVal = 0;
    	
    	for( int x = start; x <= end; x++ )
    	{
    		if( this[x] )
    		{
    			retVal += 1<<( end - x );
    		}
    	}
    	
    	return retVal;
    }
    
    /**
     * Returns the long value represented by the bit range
     * @param start - MSB of the long
     * @param end - LSB of the long
     * @return - long value of the bit range
     */
    public long getLong( int start, int end )
    {
    	long retVal = 0;
    	
    	for( int x = start; x <= end; x++ )
    	{
    		if( this[x] )
    		{
    			retVal += 1<<( end - x );
    		}
    	}
    	
    	return retVal;
    }
}

    public class BitSetFullException:Exception{
        public BitSetFullException(string msg):base(msg){}
    }
}
