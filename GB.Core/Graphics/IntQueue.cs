using System.Runtime.CompilerServices;

namespace GB.Core.Graphics
{
    internal class IntQueue
    {
        private readonly List<int> _queue;

        public IntQueue(int capacity) => _queue = new List<int>(capacity);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Size() => _queue.Count;
        
        public void Enqueue(int value) => _queue.Add(value);

        public int Dequeue()
        {
            var value = _queue[0];
            _queue.RemoveAt(0);
            return value;
        }

        public int Get(int index) => _queue[index];

        public void Clear() => _queue.Clear();

        public void Set(int index, int value) => _queue[index] = value;
    }
}