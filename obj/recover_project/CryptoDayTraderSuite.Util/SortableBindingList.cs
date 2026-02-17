using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CryptoDayTraderSuite.Util
{
	public class SortableBindingList<T> : BindingList<T>
	{
		private bool _isSorted;

		private ListSortDirection _sortDirection;

		private PropertyDescriptor _sortProperty;

		protected override bool SupportsSortingCore => true;

		protected override bool IsSortedCore => _isSorted;

		protected override ListSortDirection SortDirectionCore => _sortDirection;

		protected override PropertyDescriptor SortPropertyCore => _sortProperty;

		public SortableBindingList()
		{
		}

		public SortableBindingList(IList<T> list)
			: base(list)
		{
		}

		protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
		{
			_sortProperty = prop;
			_sortDirection = direction;
			if (base.Items is List<T> items)
			{
				items.Sort(delegate(T a, T b)
				{
					object value = prop.GetValue(a);
					object value2 = prop.GetValue(b);
					return Compare(value, value2, direction);
				});
				_isSorted = true;
			}
			else
			{
				_isSorted = false;
			}
			OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
		}

		private int Compare(object valA, object valB, ListSortDirection direction)
		{
			int result = ((valA == null) ? ((valB != null) ? (-1) : 0) : ((valB == null) ? 1 : ((!(valA is IComparable comparable)) ? ((!valA.Equals(valB)) ? valA.ToString().CompareTo(valB.ToString()) : 0) : comparable.CompareTo(valB))));
			return (direction == ListSortDirection.Ascending) ? result : (-result);
		}

		protected override void RemoveSortCore()
		{
			_isSorted = false;
			_sortProperty = null;
		}
	}
}
