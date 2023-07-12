// ****************************************************************************
// * Project:  Resource Reflector
// * File:     DictionaryBindingList.cs
// * Author:   Latency McLaughlin
// * Date:     04/19/2014
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ResourceReflector {
  /// <summary>
  /// </summary>
  /// <typeparam name="TKey"></typeparam>
  /// <typeparam name="TValue"></typeparam>
  [Serializable]
  public sealed class Pair<TKey, TValue> {
    private readonly IDictionary<TKey, TValue> _data;

    public Pair(TKey key, IDictionary<TKey, TValue> data) {
      Key = key;
      _data = data;
    }

    public TKey Key { get; }

    public TValue Value {
      get {
        TValue value;
        _data.TryGetValue(Key, out value);
        return value;
      }
      set { _data[Key] = value; }
    }
  }

  /// <summary>
  /// </summary>
  /// <typeparam name="TKey"></typeparam>
  /// <typeparam name="TValue"></typeparam>
  [Serializable]
  public class DictionaryBindingList<TKey, TValue> : BindingList<Pair<TKey, TValue>> {
    private readonly IDictionary<TKey, TValue> _data;

    /// <summary>
    /// </summary>
    /// <param name="data"></param>
    public DictionaryBindingList(IDictionary<TKey, TValue> data) {
      _data = data;
      Reset();
    }

    /// <summary>
    /// </summary>
    public void Reset() {
      var oldRaise = RaiseListChangedEvents;
      RaiseListChangedEvents = false;
      try {
        Clear();
        foreach (var key in _data.Keys)
          Add(new Pair<TKey, TValue>(key, _data));
      } finally {
        RaiseListChangedEvents = oldRaise;
        ResetBindings();
      }
    }
  }
}