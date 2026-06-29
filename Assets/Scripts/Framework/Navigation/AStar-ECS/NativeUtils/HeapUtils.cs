namespace Framework
{
    public static class HeapUtils {
        /// <summary>
        /// 计算给定节点的父节点索引。
        /// </summary>
        /// <param name="index">当前节点在堆数组中的索引（从1起）</param>
        /// <returns>父节点索引</returns>
        public static int Parent(int index) {
            return index / 2;
        }

        /// <summary>
        /// 计算给定节点的左子节点索引。
        /// </summary>
        /// <param name="index">当前节点在堆数组中的索引（从1起）</param>
        /// <returns>左子节点索引</returns>
        public static int Left(int index) {
            return index * 2;
        }

        /// <summary>
        /// 计算给定节点的右子节点索引。
        /// </summary>
        /// <param name="index">当前节点在堆数组中的索引（从1起）</param>
        /// <returns>右子节点索引</returns>
        public static int Right(int index) {
            return index * 2 + 1;
        }
    }
}