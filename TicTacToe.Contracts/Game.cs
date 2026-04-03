using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe.Contracts
{
    [DataContract]
    public class Game
    {
        private static Random _random = new Random();

        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public Board Board { get; set; } = new Board();

        [DataMember]    
        public Guid? PlayerX { get; set; }

        [DataMember]
        public Guid? PlayerO { get; set; }

        [DataMember]
        public bool XTurn { get; set; }

        public bool TurnOf(Guid playerId)
        {
            if (playerId == PlayerX)
                return XTurn;

            if (playerId == PlayerO)
                return !XTurn;

            throw new ArgumentException($"Wrong player id: {nameof(playerId)}");
        }

        public string Display()
        {
            return $"id={Id}\r\nPlayerX={PlayerX}\r\nPlayerO={PlayerO}\r\nXTurn={XTurn}\r\nBoard:\r\n{Board.Display()}";
        }

        public Game SetRandom(Guid player1, Guid player2)
        {
            if (_random.NextDouble() < 0.5)
            {
                PlayerX = player1;
                PlayerO = player2;
            }
            else
            {
                PlayerX = player2;
                PlayerO = player1;
            }

            XTurn = _random.NextDouble() < 0.5;
            return this;
        }

        public void MakeMove(Guid playerId, int position)
        {
            // check args
            if (position < 0 || position > 8)
                throw new ArgumentOutOfRangeException(nameof(position));

            if (Board.Fields[position] != FieldValue.Empty)
                throw new ArgumentException("Field is not empty");

            FieldValue value;
            if (playerId == PlayerX)
                value = FieldValue.X;
            else if (playerId == PlayerO)
                value = FieldValue.O;
            else
                throw new ArgumentException($"Wrong player id: {nameof(playerId)}");

            if (Board.IsFinished)
                throw new Exception("Game is finihed");

            if (value == FieldValue.X && false == XTurn)
                throw new Exception("It's not X's turn");

            if (value == FieldValue.O && true == XTurn)
                throw new Exception("It's not O's turn");

            Board.Fields[position] = value;
            XTurn = !XTurn;
        }
    }
}
