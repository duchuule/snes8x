using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace PhoneDirect3DXamlAppInterop.Database
{
    //http://stackoverflow.com/questions/1427471/observablecollection-not-noticing-when-item-in-it-changes-even-with-inotifyprop

    public class TrulyObservableCollection<T> : ObservableCollection<T>
where T : INotifyPropertyChanged
    {
        public TrulyObservableCollection()
            : base()
        {
            CollectionChanged += new NotifyCollectionChangedEventHandler(TrulyObservableCollection_CollectionChanged);
        }

        public TrulyObservableCollection(IEnumerable<T> collection)
            : base(collection)
        {
            //add event for each item in the collection
            foreach (Object item in collection)
            {
                (item as INotifyPropertyChanged).PropertyChanged += new PropertyChangedEventHandler(item_PropertyChanged);
            }
            CollectionChanged += new NotifyCollectionChangedEventHandler(TrulyObservableCollection_CollectionChanged);
        }

        void TrulyObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Object item in e.NewItems)
                {
                    (item as INotifyPropertyChanged).PropertyChanged += new PropertyChangedEventHandler(item_PropertyChanged);
                }
            }
            if (e.OldItems != null)
            {
                foreach (Object item in e.OldItems)
                {
                    (item as INotifyPropertyChanged).PropertyChanged -= new PropertyChangedEventHandler(item_PropertyChanged);
                }
            }
        }

        public void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, IndexOf((T)sender));
            //NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(a);
        }
    }
}
