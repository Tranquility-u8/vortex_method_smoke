namespace GPUSmoke
{
    public struct BitOperation
    {
        public static System.Int32 FindMSB(System.Int32 x)
        {
            if (x == 0)
                return -1;
            System.Int32 r = 0;
            while ((x >>= 1) != 0)
                ++r;
            return r;
        }
    }
}