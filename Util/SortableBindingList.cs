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

        public SortableBindingList() : base() { }
        public SortableBindingList(IList<T> list) : base(list) { }

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => _isSorted;
        protected override ListSortDirection SortDirectionCore => _sortDirection;
        protected override PropertyDescriptor SortPropertyCore => _sortProperty;

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            _sortProperty = prop;
            _sortDirection = direction;

            if (Items is List<T> items)
            {
                items.Sort((a, b) => {
                    var valA = prop.GetValue(a);
                    var valB = prop.GetValue(b);
                    return Compare(valA, valB, direction);
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
            int result;
            if (valA == null)
                result = (valB == null) ? 0 : -1;
            else if (valB == null)
                result = 1;
            else if (valA is IComparable comparable)
                result = comparable.CompareTo(valB);
            else if (valA.Equals(valB))
                result = 0;
            else
                result = valA.ToString().CompareTo(valB.ToString());

            return (direction == ListSortDirection.Ascending) ? result : -result;
        }

        protected override void RemoveSortCore()
        {
            _isSorted = false;
            _sortProperty = null;
        }
    }
}