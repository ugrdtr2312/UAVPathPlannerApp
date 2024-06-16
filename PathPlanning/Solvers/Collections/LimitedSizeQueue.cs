namespace PathPlanning.Solvers.Collections;

public class LimitedSizeQueue<T>
{
    private readonly Queue<T> _queue = new();
    private readonly int _maxSize;

    public LimitedSizeQueue(int maxSize)
    {
        if (maxSize <= 0)
        {
            throw new ArgumentException("Maximum size must be greater than zero.");
        }

        _maxSize = maxSize;
    }

    public void Enqueue(T item)
    {
        if (_queue.Count >= _maxSize)
        {
            _queue.Dequeue();
        }

        _queue.Enqueue(item);
    }

    public T Dequeue()
    {
        return _queue.Dequeue();
    }

    public T Peek()
    {
        return _queue.Peek();
    }

    public bool Contains(T item)
    {
        return _queue.Contains(item);
    }

    public int Count => _queue.Count;
}