using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DropDownStringDatabase", menuName = "ScriptableObjects/DropDownStringDatabase", order = 1)]
public class DropDownStringDatabase : ScriptableObject
{
    [Serializable]
    public class DropDownStringEntry
    {
        [SerializeField] string _key;
        [SerializeField] List<string> _options = new List<string>();
        public string Key => _key;
        public List<string> Options => _options;

        public DropDownStringEntry(string key, List<string> options)        {
            _key = key;
            _options = options;
        }
    }
    [SerializeField] List<DropDownStringEntry> _entries = new List<DropDownStringEntry>();
    private Dictionary<string, List<string>> _lookup;

    void OnValidate()
    {
        RegenerateLookup();
    }

    public List<string> GetOptions(string key)
    {
        if (_lookup != null && _lookup.TryGetValue(key, out var options))
        {
            return options;
        }
        return new List<string>();
    }
    public List<string> GetOptions(DropdownStringAttribute attribute)
    {
        return GetOptions(attribute.Key);
    }
    public List<string> GetKeys()
    {
        if (_lookup != null)
        {
            return _lookup.Keys.ToList();
        }
        return new List<string>();
    }
    public void AddOption(string key, string option)
    {
        foreach (var entry in _entries)
        {
            if (entry.Key == key)
            {
                if (!entry.Options.Contains(option))
                {
                    entry.Options.Add(option);
                    RegenerateLookup();
                }
                return;
            }
        }
        // If key not found, create a new entry
        var newEntry = new DropDownStringEntry(key, new List<string> { option });
        _entries.Add(newEntry);
        RegenerateLookup();
    }

    void RegenerateLookup()
    {
        _lookup = new Dictionary<string, List<string>>();
        foreach (var entry in _entries)
        {
            _lookup[entry.Key] = new List<string>(entry.Options);
        }
    }
}