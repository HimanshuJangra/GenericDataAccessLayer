using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Repository.Calls
{
    public class User
    {
        public int Id { get; set; }

        public string UserName { get; set; }
        
        public DateTime Created { get; set; }

        public bool IsActive { get; set; }

        public byte[] Avatar { get; set; }
    }
}
