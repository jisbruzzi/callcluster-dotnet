﻿using System.Collections.Generic;

namespace callcluster_dotnet.old
{
    internal class SolutionDTO
    {
        public SolutionDTO()
        {
        }

        public string FilePath { get; internal set; }
        public IList<ProjectDTO> Projects { get; internal set; }
    }
}