using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASong
{
    // The AST, as it were.
    #region Pseudo-AST
    public class Command
    {
        public byte Value;
        public uint RelativeOffset;

        public Command(byte value)
        {
            Value = value;
        }

        public int GetSize()
        {
            return 1;
        }
    }

    public class Jump : Command
    {
        public uint Pointer;
        public uint RelativePointer;

        public Jump(byte value)
            : base(value)
        {
            Pointer = 0;
            RelativePointer = 0;
        }

        public new int GetSize()
        {
            return 5;
        }
    }

    public class Repeat : Command
    {
        public List<byte> Arguments;

        public Repeat()
            : base(0) // For now, a place holder.
        {
            Arguments = new List<byte>();
        }

        public new int GetSize()
        {
            return Arguments.Count;
        }
    }

    public class Generic : Command
    {
        public List<byte> Arguments;

        public Generic(byte value)
            : base(value)
        {
            Arguments = new List<byte>();
        }

        public new int GetSize()
        {
            return 1 + Arguments.Count;
        }
    }

    public class xCommand : Command
    {
        public uint xValue;

        public xCommand(byte value, uint x)
            : base(value)
        {
            xValue = x;
        }

        public new int GetSize()
        {
            return 5;
        }
    }
    #endregion
}
