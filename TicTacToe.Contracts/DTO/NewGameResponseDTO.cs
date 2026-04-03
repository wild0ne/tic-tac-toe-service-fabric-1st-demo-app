using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe.Contracts.DTO
{
    [DataContract]
    public class NewGameResponseDTO
    {
        [DataMember]
        public bool Created { get; set; }

        [DataMember]
        public Game? Game { get; set; }
    }
}
