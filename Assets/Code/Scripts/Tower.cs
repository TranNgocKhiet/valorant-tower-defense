using UnityEngine;
using System;

[Serializable]
public class Tower
{
    public string name;
    public GameObject prefab;
    public int cost;

    public Tower(string _name, GameObject _prefab, int _cost)
    {
        this.name = _name;
        this.prefab = _prefab;
        this.cost = _cost;
    }
}
