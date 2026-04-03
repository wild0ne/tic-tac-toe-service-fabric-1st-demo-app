using System.Runtime.Serialization;

namespace TicTacToe.Contracts
{
    [DataContract]
    public class Board
    {
        const int FIELDS_COUNT = 9;

        public Board()
        {
            Fields = Enumerable.Range(0, FIELDS_COUNT).Select(_ => FieldValue.Empty).ToList();
        }

        [DataMember]
        public List<FieldValue> Fields { get; set; }

        public bool IsFinished => this.Fields.Count > 0 && this.Fields.All(f => f != FieldValue.Empty);

        public FieldValue GetWinner()
        {
            // rows

            if (Get(0, 0) == Get(0, 1) && Get(0, 1) == Get(0, 2))
                return Get(0, 0);

            if (Get(1, 0) == Get(1, 1) && Get(1, 1) == Get(1, 2))
                return Get(1, 0);

            if (Get(2, 0) == Get(2, 1) && Get(2, 1) == Get(2, 2))
                return Get(2, 0);

            // cols

            if (Get(0, 0) == Get(1, 0) && Get(1, 0) == Get(2, 0))
                return Get(0, 0);

            if (Get(0, 1) == Get(1, 1) && Get(1, 1) == Get(2, 1))
                return Get(0, 1);

            if (Get(0, 2) == Get(1, 2) && Get(1, 2) == Get(2, 2))
                return Get(0, 2);

            // diagonals

            if (Get(0, 0) == Get(1, 1) && Get(1, 1) == Get(2, 2))
                return Get(0, 0);

            if (Get(0, 2) == Get(1, 1) && Get(1, 1) == Get(2, 0))
                return Get(0, 2);

            return FieldValue.Empty;
        }

        public string GetWinnerText()
        {
            switch(GetWinner())
            {
                case FieldValue.Empty: return "TIE";
                case FieldValue.X: return "X wins";
                case FieldValue.O: return "O wins";
                default:
                    throw new NotImplementedException();
            }
        }

        private FieldValue Get(int row, int col) => Fields[row * 3 + col];

        public string Display()
        {
            return $"{Fields[0].ToSymbol()} {Fields[1].ToSymbol()} {Fields[2].ToSymbol()}\r\n"
                + $"{Fields[3].ToSymbol()} {Fields[4].ToSymbol()} {Fields[5].ToSymbol()}\r\n"
                + $"{Fields[6].ToSymbol()} {Fields[7].ToSymbol()} {Fields[8].ToSymbol()}";
        }
    }
}

