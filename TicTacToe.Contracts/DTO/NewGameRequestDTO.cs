using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe.Contracts.DTO
{
    [DataContract]
    public class NewGameRequestDTO
    {
        [DataMember]
        public Guid PlayerId { get; set; }
    }
}
