using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class ObservableStack<T> : INotifyCollectionChanged, IEnumerable<T>, ICollection
	{
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _internalStack.GetEnumerator();
		}

		public IEnumerator GetEnumerator()
		{
			return _internalStack.GetEnumerator();
		}

		public void Clear()
		{
			_internalStack.Clear();
			InvokeCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public bool Contains(T item)
		{
			return _internalStack.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_internalStack.CopyTo(array, arrayIndex);
		}

		public void TrimExcess()
		{
			_internalStack.TrimExcess();
		}

		public T Peek()
		{
			return _internalStack.Peek();
		}

		public T Pop()
		{
			var item = _internalStack.Pop();
			InvokeCollectionChanged(
				new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, 
					item, _internalStack.Count));
			return item;
		}
		
		public void Push(T item)
		{
			_internalStack.Push(item);
			InvokeCollectionChanged(
				new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
					item, _internalStack.Count - 1));
		}

		public T[] ToArray()
		{
			return _internalStack.ToArray();
		}

		public void CopyTo(Array array, int index)
		{
			((ICollection)_internalStack).CopyTo(array, index);
		}

		public int Count
		{
			get { return _internalStack.Count; }
		}

		public object SyncRoot
		{
			get { return ((ICollection) _internalStack).SyncRoot; }
		}

		public bool IsSynchronized
		{
			get { return ((ICollection) _internalStack).IsSynchronized; }
		}

		private readonly Stack<T> _internalStack;

		public ObservableStack(Stack<T> stack)
		{
			_internalStack = stack;
		}

		public ObservableStack()
		{
			_internalStack = new Stack<T>();
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public void InvokeCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			NotifyCollectionChangedEventHandler handler = CollectionChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}
	}
}