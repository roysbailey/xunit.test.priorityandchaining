using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoN.API
{
    public class SoNFacade : ISoNAPI
    {
        public int CreateSoN(string TLB, string SoNTitle)
        {
            var sonID = -1;
            switch(TLB)
            {
                case "FINANCE":
                    sonID = 1;
                    break;
                case "COMMERCIAL":
                    sonID = 2;
                    break;
                case "MARKETING":
                    sonID = 3;
                    break;
                case "ACCOUNTING":
                    sonID = 4;
                    break;
                case "SALES":
                    sonID = 5;
                    break;
            }

            return sonID;
        }
    }
}
