using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe.Contracts
{
    public enum FieldValue : byte
    {
        Empty = 0,
        X = 1,
        O = 2,
    }

    public static class FieldValueHelper
    {
        public static string ToSymbol(this FieldValue value)
        {
            return value switch
            {
                FieldValue.Empty => ".",
                FieldValue.X => "X",
                FieldValue.O => "O",
                _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unexpected value: {value}"),
            };
        }
    }
}
