//------------------------------------------------------------------------------
//      Copyright (c) Microsoft Corporation.  All rights reserved.                                                             
//------------------------------------------------------------------------------

using System;
using System.Collections;

using OrganismBase;
using Terrarium.Configuration;
using Terrarium.Game;
using Terrarium.Tools;
using Terrarium.Forms;

namespace Terrarium.Hosting 
{
    // GameObjectCollection is a collection of animals currently hosted
    // by the GameScheduler.
    [Serializable]
    internal class GameObjectCollection : ICollection, IList, IEnumerable 
    {
        ArrayList _list;
        Hashtable _orgMap;

        public GameObjectCollection()
        {
            _list = new ArrayList();
            _orgMap = new Hashtable();
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsSynchronized
        {
            get 
            {
                return _list.IsSynchronized;
            }
        }

        public object SyncRoot
        {
            get 
            {
                return _list.SyncRoot;
            }
        }

        public void CopyTo(Array array, int index)
        {
            _list.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator() 
        {
            return new SimpleSequentialEnumerator(_list);
        }

        public bool IsFixedSize 
        {
            get
            {
                return false;
            }
        }

        public bool IsReadOnly 
        {
            get 
            {
                return false;
            }
        }

        public object this[int index]
        {
            get 
            { 
                return _list[index];
            }
        
            set 
            {
                _list[index] = value;
            }
        }

        public object this[string index]
        {
            get
            {
                if (_orgMap.Contains(index))
                {
                    return ((OrganismWrapper)_orgMap[index]).Organism;
                }
                return null;
            }
        }

        public OrganismWrapper GetWrapperForOrganism(string id)
        {
            if (_orgMap.Contains(id))
            {
                return (OrganismWrapper)_orgMap[id];
            }

            return null;
        }

        public int Add(object value)
        {
            OrganismWrapper wrap = (OrganismWrapper)value;
            _orgMap.Add(wrap.Organism.ID, wrap);
            return _list.Add(value);
        }

        public void Clear()
        {
            _list.Clear();
            _orgMap.Clear();
        }

        public Boolean Contains(object value)
        {
            return _list.Contains(value);
        }

        public Boolean ContainsKey(object value)
        {
            return _orgMap.ContainsKey(value);
        }

        public int IndexOf(object value)
        {
            return _list.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            _list.Insert(index, value);
        }

        public void Remove(object value)
        {
            if (value is string)
            {
                string strVal = value as string;
                if (_orgMap.Contains(strVal))
                {
                    _orgMap.Remove(strVal);
                }

                int ndx = -1;
                OrganismWrapper wrap;
                for (int i = 0; i < _list.Count; i++)
                {
                    wrap = (OrganismWrapper)_list[i];
                    if (wrap.Organism.ID == strVal)
                    {
                        ndx = i;
                        break;
                    }
                }

                if (-1 != ndx)
                {
                    _list.RemoveAt(ndx);
                }
            }
            else
            {
                _list.Remove(value);
            }
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        // After being deserialized we need to reset the world boundaries since they aren't serialized.
        // We also deserialize each organism
        public void CompleteOrganismDeserialization()
        {
            foreach (DictionaryEntry e in _orgMap)
            {
                Organism organism = ((OrganismWrapper) e.Value).Organism;
                OrganismWorldBoundary.SetWorldBoundary(organism, (string) e.Key);
                try
                {
                    if (organism is Animal)
                    {
                        ((Animal) organism).DeserializeAnimal(organism.SerializedStream);
                    }
                    else
                    {
                        ((Plant) organism).DeserializePlant(organism.SerializedStream);
                    }
                }
                catch (Exception exception)
                {
                    ErrorLog.LogHandledException(exception);

                    if (GameEngine.Current != null)
                    {
                        GameEngine.Current.OnEngineStateChanged(new EngineStateChangedEventArgs(EngineStateChangeType.Other,
                            "Organism Deserialization Failure.",
                            organism.GetType().Assembly.GetName().Name + "refuses to come out of Cryogenic Stasis until a cure is found for Terrarium Syndrome (aka Deserialization Failure)"));
                    }
                }

                organism.SerializedStream = null;
            }
        }
    }
}