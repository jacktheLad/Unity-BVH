using System.Collections;
using System.Collections.Generic;

namespace sif
{
    public class CPU_SBVHBuilder
    {
        const float SPLIT_ALPHA = 1.0E-5F;

        private CPU_SBVHData _bvh;
        private CPU_SBVHBuilder(CPU_SBVHData bvh)
        {
            _bvh = bvh;
        }

        public static void Build(CPU_SBVHData bvh)
        {
            var builder = new CPU_SBVHBuilder(bvh);
        }

    }
}

