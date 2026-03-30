using System;

namespace Balda.Features.Game.Domain
{
    [Serializable]
    public struct BoardPosition : IEquatable<BoardPosition>
    {
        public int Row;
        public int Col;

        public BoardPosition(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool Equals(BoardPosition other)
        {
            return Row == other.Row && Col == other.Col;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Row * 397) ^ Col;
            }
        }
    }
}
