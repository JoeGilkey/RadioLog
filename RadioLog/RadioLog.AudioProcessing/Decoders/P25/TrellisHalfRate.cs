using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.AudioProcessing.Decoders.P25
{
    public class TrellisHalfRate
    {
        private static readonly int[][] CONSTELLATION_COSTS = 
		{ new int[]{ 0,1,1,2,1,2,2,3,1,2,2,3,2,3,3,4 },
		  new int[]{ 1,0,2,1,2,1,3,2,2,1,3,2,3,2,4,3 },
		  new int[]{ 1,2,0,1,2,3,1,2,2,3,1,2,3,4,2,3 },
		  new int[]{ 2,1,1,0,3,2,2,1,3,2,2,1,4,3,3,2 },
		  new int[]{ 1,2,2,3,0,1,1,2,2,3,3,4,1,2,2,3 },
		  new int[]{ 2,1,3,2,1,0,2,1,3,2,4,3,2,1,3,2 },
		  new int[]{ 2,3,1,2,1,2,0,1,3,4,2,3,2,3,1,2 },
		  new int[]{ 3,2,2,1,2,1,1,0,4,3,3,2,3,2,2,1 },
		  new int[]{ 1,2,2,3,2,3,3,4,0,1,1,2,1,2,2,3 },
		  new int[]{ 2,1,3,2,3,2,4,3,1,0,2,1,2,1,3,2 },
		  new int[]{ 2,3,1,2,3,4,2,3,1,2,0,1,2,3,1,2 },
		  new int[]{ 3,2,2,1,4,3,3,2,2,1,1,0,3,2,2,1 },
		  new int[]{ 2,3,3,4,1,2,2,3,1,2,2,3,0,1,1,2 },
		  new int[]{ 3,2,4,3,2,1,3,2,2,1,3,2,1,0,2,1 },
		  new int[]{ 3,4,2,3,2,3,1,2,2,3,1,2,1,2,0,1 },
		  new int[]{ 4,3,3,2,3,2,2,1,3,2,2,1,2,1,1,0 } };
        private List<ConstellationNode> mConstellationNodes = new List<ConstellationNode>();

        public TrellisHalfRate()
        {
            ConstellationNode previous = null;
            for (int x = 0; x < 49; x++)
            {
                ConstellationNode node = new ConstellationNode();
                if (previous != null)
                {
                    previous.connect(node);
                }
                previous = node;
                mConstellationNodes.Add(node);
            }
        }

        public void decode(RadioLog.Common.SafeBitArray message, int start, int end)
        {
            /* load each of the nodes with deinterleaved constellations */
            for (int index = 0; index < 49; index++)
            {
                Constellation c = getConstellation(message, index * 4);

                mConstellationNodes[index].setConstellation(c);
            }

            /* test to see if constellations are correct - otherwise correct them */
            ConstellationNode firstNode = mConstellationNodes[0];

            if (!firstNode.startsWith(Dibit.D0) || !firstNode.isCorrect())
            {
                firstNode.correctTo(Dibit.D0);
            }

            /* clear constellations from original message */
            message.ClearRange(start, end - start);

            /* replace with decoded values from the nodes */
            for (int index = 0; index < 49; index++)
            {
                ConstellationNode node = mConstellationNodes[index];

                if (node.firstBit())
                {
                    message.SetBit(start + (index * 2));
                }
                if (node.secondBit())
                {
                    message.SetBit(start + (index * 2) + 1);
                }
            }
        }
        private Constellation getConstellation(RadioLog.Common.SafeBitArray message, int index)
        {
            int constellation = 0;

            for (int x = 0; x < 4; x++)
            {
                if (message[index + x])
                {
                    constellation += (1 << (3 - x));
                }
            }

            return Constellation.fromValue(constellation);
        }

        public class ConstellationNode
        {
            private ConstellationNode mConnectedNode;
            private Constellation mConstellation;
            private bool mCorrect;

            public ConstellationNode()
            {
            }

            public void dispose()
            {
                mConnectedNode = null;
            }

            public bool startsWith(Dibit dibit)
            {
                return mConstellation.getLeft() == dibit;
            }

            public bool firstBit()
            {
                return mConstellation.getRight().firstBit();
            }

            public bool secondBit()
            {
                return mConstellation.getRight().secondBit();
            }

            /**
             * Executes a correction down the line of connected nodes.  Only nodes
             * with the mCorrect flag set to false will be corrected.
             * 
             * Note: Assumes that the starting node's value is 0.  Initiate the 
             * corrective sequence by invoking this method with Dibit.D0 on the
             * first node.
             * 
             * @param dibit to use for the left side.
             */
            public void correctTo(Dibit dibit)
		{
			if( mCorrect && mConstellation.getLeft() == dibit )
			{
				return;
			}
			
			if( isCurrentConnectionCorrect() )
			{
				mConstellation = Constellation.
						fromDibits( dibit, mConstellation.getRight() );

				mCorrect = true;
				
				if( mConnectedNode != null )
				{
					mConnectedNode.correctTo( mConstellation.getRight() );
				}
			}
			else
			{
				Constellation cheapest = mConstellation;
				
				int cost = 100; //arbitrary
				
				foreach( Dibit d in Dibit.values() )
				{
					Constellation test = Constellation.fromDibits( dibit, d );
					
					int testCost = mConstellation.costTo( test ) + 
								   mConnectedNode.costTo( d );

					if( testCost < cost )
					{
						cost = testCost;
						cheapest = test;
					}
				}

				mConstellation = cheapest;
				
				mConnectedNode.correctTo( mConstellation.getRight() );
				
				mCorrect = true;
			}
		}

            /**
             * Calculates the cost (hamming distance) of using the argument as the
             * left side dibit for the current node, and recursively finding the
             * cheapest corresponding right dibit.
             * 
             * @param leftTest
             * @return
             */
            public int costTo(Dibit leftTest)
            {
                if (isCurrentConnectionCorrect())
                {
                    Constellation c = Constellation.
                            fromDibits(leftTest, mConstellation.getRight());

                    return mConstellation.costTo(c);
                }
                else
                {
                    int cheapestCost = 100; //arbitrary

                    foreach (Dibit d in Dibit.values())
                    {
                        Constellation c = Constellation.fromDibits(leftTest, d);

                        int cost = mConnectedNode.costTo(d) +
                                   mConstellation.costTo(c);

                        if (cost < cheapestCost)
                        {
                            cheapestCost = cost;
                        }
                    }

                    return cheapestCost;
                }
            }

            /**
             * Indicates if the immediate connection is correct
             */
            public bool isCurrentConnectionCorrect()
            {
                return (mConnectedNode == null ||
                         mConstellation.getRight() == mConnectedNode.getLeft());
            }

            /**
             * Executes a recursive call to all nodes to the right, setting the 
             * mCorrect flag on all nodes to true, if the node's connection to the
             * right node is correct and all nodes to the right are correct.  Or,
             * false if this node's connection, or any node's connection to the
             * right is incorrect.
             * 
             * @return - true - all node connections to the right are correct
             * 			 false - one or more nodes to the right are incorrect
             */
            public bool isCorrect()
            {
                if (mCorrect)
                {
                    return mCorrect;
                }

                if (mConnectedNode == null)
                {
                    mCorrect = true;
                }
                else
                {
                    mCorrect = mConnectedNode.isCorrect() &&
                        mConstellation.getRight() == mConnectedNode.getLeft();
                }

                return mCorrect;
            }

            public Dibit getLeft()
            {
                return mConstellation.getLeft();
            }

            public void setConstellation(Constellation constellation)
            {
                mConstellation = constellation;
                mCorrect = false;
            }

            public void connect(ConstellationNode node)
            {
                mConnectedNode = node;
            }
        }

        public class Constellation
        {
            public static readonly Constellation C0 = new Constellation(Dibit.D1, Dibit.D1, 0);
            public static readonly Constellation C1 = new Constellation(Dibit.D0, Dibit.D2, 1);
            public static readonly Constellation C2 = new Constellation(Dibit.D0, Dibit.D0, 2);
            public static readonly Constellation C3 = new Constellation(Dibit.D1, Dibit.D3, 3);
            public static readonly Constellation C4 = new Constellation(Dibit.D2, Dibit.D3, 4);
            public static readonly Constellation C5 = new Constellation(Dibit.D3, Dibit.D0, 5);
            public static readonly Constellation C6 = new Constellation(Dibit.D3, Dibit.D2, 6);
            public static readonly Constellation C7 = new Constellation(Dibit.D2, Dibit.D1, 7);
            public static readonly Constellation C8 = new Constellation(Dibit.D3, Dibit.D3, 8);
            public static readonly Constellation C9 = new Constellation(Dibit.D2, Dibit.D0, 9);
            public static readonly Constellation CA = new Constellation(Dibit.D2, Dibit.D2, 10);
            public static readonly Constellation CB = new Constellation(Dibit.D3, Dibit.D1, 11);
            public static readonly Constellation CC = new Constellation(Dibit.D0, Dibit.D1, 12);
            public static readonly Constellation CD = new Constellation(Dibit.D1, Dibit.D2, 13);
            public static readonly Constellation CE = new Constellation(Dibit.D1, Dibit.D0, 14);
            public static readonly Constellation CF = new Constellation(Dibit.D0, Dibit.D3, 15);

            private Dibit mLeftDibit;
            private Dibit mRightDibit;
            private int mValue;

            public Constellation(Dibit leftDibit, Dibit rightDibit, int value)
            {
                mLeftDibit = leftDibit;
                mRightDibit = rightDibit;
                mValue = value;
            }

            public Dibit getLeft() { return mLeftDibit; }
            public Dibit getRight() { return mRightDibit; }
            public int getValue() { return mValue; }

            public static Constellation fromValue(int value)
            {
                if (0 <= value && value <= 15)
                    return Constellation.values()[value];
                return null;
            }

            public int costTo(Constellation other)
            {
                return CONSTELLATION_COSTS[getValue()][other.getValue()];
            }

            public static Constellation fromDibits(Dibit left, Dibit right)
            {
                if (Dibit.Equals(left, Dibit.D0))
                {

                }
                else if (Dibit.Equals(left, Dibit.D1))
                {

                }
                else if (Dibit.Equals(left, Dibit.D2))
                {

                }
                else
                {
                    //
                }
                return null;
            }

            public static Constellation[] values()
            {
                return new Constellation[] { C0, C1, C2, C3, C4, C5, C6, C7, C8, C9, CA, CB, CC, CD, CE, CF };
            }
        }

        public class Dibit
        {
            public static readonly Dibit D0 = new Dibit(false, false);
            public static readonly Dibit D1 = new Dibit(false, true);
            public static readonly Dibit D2 = new Dibit(true, false);
            public static readonly Dibit D3 = new Dibit(true, true);

            private bool mFirstBit;
            private bool mSecondBit;

            private Dibit(bool firstBit, bool secondBit)
            {
                mFirstBit = firstBit;
                mSecondBit = secondBit;
            }

            public bool firstBit()
            {
                return mFirstBit;
            }

            public bool secondBit()
            {
                return mSecondBit;
            }

            public static bool Equals(Dibit first, Dibit second)
            {
                if (first == null || second == null)
                    return false;
                else
                    return (first.firstBit() == second.firstBit() && first.secondBit() == second.secondBit());
            }

            public static Dibit[] values() { return new Dibit[] { D0, D1, D2, D3 }; }
        }
    }
}
