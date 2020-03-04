using System.Collections;
using System.Collections.Generic;

namespace sif
{

    public class CPU_SBVHBuilder
    {
        // 写这么长是因为C#里面值类型（struct）指定默认(非0)初始化值没有更好的方法了。
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

            public static PrimitiveRef New() 
            {
                return new PrimitiveRef(-1, AABB.New());
            }
        }

        /// <summary>
        /// 一个简化的Node，不包含子Node
        /// </summary>
        struct NodeSpec
        {
            public int NumRef { get; set; }
            public AABB Bounds { get; set; }
        }
         
        struct ObjectSplit
        {
            public float SAH { get; set; }
            public byte Dim { get; set; }
            public int NumLeftRef { get; set; }
            public AABB LeftBounds { get; set; }
            public AABB RightBounds { get; set; }
            private ObjectSplit(int nothing)
            {
                SAH = float.MaxValue;
                Dim = 0;
                NumLeftRef = 0;
                LeftBounds = AABB.New();
                RightBounds = AABB.New();
            }

            public static ObjectSplit New()
            {
                return new ObjectSplit(0);
            }
        }

        struct SpatialSplit
        {
            public float SAH { get; set; }
            public byte Dim { get; set; }
            public float Pos { get; set; }
            private SpatialSplit(int nothing)
            {
                SAH = float.MaxValue;
                Dim = 0;
                Pos = 0f;
            }
            public static SpatialSplit New()
            {
                return new SpatialSplit(0);
            }
        }

        struct SpatialBin
        {
            public AABB Bounds { get; set; }
            public int Enter { get; set; }
            public int Exit { get; set; }

            private SpatialBin(int nothing)
            {
                Bounds = AABB.New();
                Enter = 0;
                Exit = 0;
            }
            public static SpatialBin New()
            {
                return new SpatialBin(0);
            }
        }

        private const float SPLIT_ALPHA = 1.0E-5F;
        private const int N_SPATIAL_BINS = 32;
        private const int MAX_DEPTH = 64;
        private const int MAX_SPATIAL_DEPTH = 48;

        private CPU_SBVHData _bvhData;
        private List<PrimitiveRef> _refStack; // A linear stack.
        private float _totalArea;
        private List<AABB> _rightBounds;
        private byte _sortDim;
        private SpatialBin[,] _bins = new SpatialBin[3, N_SPATIAL_BINS];
        private int _numDuplicates;

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

