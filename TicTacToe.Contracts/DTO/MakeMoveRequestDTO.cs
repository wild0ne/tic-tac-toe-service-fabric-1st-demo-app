using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe.Contracts.DTO
{
    [DataContract]
    public class MakeMoveRequestDTO
    {
        [DataMember]
        public Guid GameId { get; set; }

        [DataMember] 
        public Guid PlayerId { get; set; }

        [DataMember] 
        public int Position { get; set; }
    }
}
