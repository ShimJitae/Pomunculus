using System;
using System.Collections.Generic;
using System.Linq;

public class ObjContainer<T> where T : IdOwner
{
    public virtual void Add(T item)
    {
        m_items.Add(item);
        onAddItem?.Invoke(item);
    }
    public virtual void Remove(T item)
    {
        if(m_items.Remove(item))
        {
            onRemoveItem?.Invoke(item);
        }
    }
    public virtual void Remove(int id)
    {
        var i = m_items.Find(x => x.ID == id);
        if(i != null) Remove(i);
    }
    public virtual void Clear(bool isTriggerEvent = false)
    {
        var t = GetList();
        m_items.Clear();
        foreach(var i in t) onRemoveItem?.Invoke(i);
    }
    public List<T> GetList() => m_items.ToList();
    public event Action<T> onAddItem;
    public event Action<T> onRemoveItem;
    private List<T> m_items = new ();
}