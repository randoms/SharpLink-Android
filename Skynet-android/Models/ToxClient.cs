﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkynetAndroid.Models
{
    class ToxClient
    {
        public string Id { get; set; }
        public List<NodeInfo> nodes { get; set; }
    }
}
