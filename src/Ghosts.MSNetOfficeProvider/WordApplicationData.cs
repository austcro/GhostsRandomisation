using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ghosts.MSNetOfficeProvider
{
    public class WordApplicationData
    {
        public NetOffice.WordApi.Application wordApplication { get; set; }
        public NetOffice.WordApi.Document newDocument { get; set; }
    }
}
