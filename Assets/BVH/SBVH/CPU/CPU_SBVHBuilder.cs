using System.Collections;
using System.Collections.Generic;

namespace sif
{

    public class CPU_SBVHBuilder
    {
        const float SPLIT_ALPHA = 1.0E-5F;

        // 写这么长是因为C#里面值类型（struct）指定默认初始化值没有更好的方法了。
        struct PrimitiveRef
        {
            private int _triangleIdx;
            private AABB _bounds;

            public int TriangleIdx { get => _triangleIdx; set => _triangleIdx = value; }
            public AABB Bounds { get => _bounds; set => _bounds = value; }

            private PrimitiveRef(int triIdx, AABB bounds)
            {
                _triangleIdx = triIdx;
                _bounds = bounds;
            }

            public static PrimitiveRef New() {
                return new PrimitiveRef(-1, AABB.New());
            }

            public static PrimitiveRef New(int triIdx, AABB bounds)
            {
                return new PrimitiveRef(triIdx, bounds);
            }
        }

         

        private CPU_SBVHData _bvhData;
        private CPU_SBVHBuilder(CPU_SBVHData bvhData)
        {
            _bvhData = bvhData;
        }

        public static void Build(CPU_SBVHData bvhData)
        {
            var builder = new CPU_SBVHBuilder(bvhData);


        }

    }
}

