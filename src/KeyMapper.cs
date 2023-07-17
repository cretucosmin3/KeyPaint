using System.Collections.Generic;
using System;
using Silk.NET.Input;
using System.Linq;

namespace KeyPaint;

public class KeyMapper
{
    private readonly List<Key> ControlKeys = new()
    {
        Key.ControlLeft,
        Key.ControlRight,
        Key.AltLeft,
        Key.AltRight
    };

    private bool IsHotkey;
    private readonly List<Key> KeySequence = new();

    private readonly Dictionary<string, KeyUpRestriction> KeyUpRestrictions = new();
    private readonly Dictionary<string, KeyBind> KeyDownBinds = new();
    private readonly Dictionary<string, KeyBind> KeyUpBinds = new();
    private readonly Dictionary<string, KeyBind> HotkeysDown = new();
    private readonly Dictionary<string, KeyBind> HotkeysUp = new();

    public event Action<char> OnKeyType = default!;

    public void AddKeyUpRestriction(Key[] keys)
    {
        KeyUpRestriction restriction = new(keys);

        foreach (Key key in keys)
        {
            KeyUpRestrictions.Add(key.ToString(), restriction);
        }
    }

    public KeyBind OnHotkeyDown(Key[] hkKeys)
    {
        string hotkeyName = GetHotkeyName(hkKeys);

        if (HotkeysDown.ContainsKey(hotkeyName))
            throw new Exception($"Hotkey down bind {hotkeyName} was already assigned once.");

        ExtractControlKey(hkKeys);

        KeyBind newHotkey = new();
        HotkeysDown.Add(hotkeyName, newHotkey);

        return newHotkey;
    }

    public KeyBind OnHotkeyUp(Key[] hkKeys)
    {
        string hotkeyName = GetHotkeyName(hkKeys);

        if (HotkeysUp.ContainsKey(hotkeyName))
            throw new Exception($"Hotkey up bind {hotkeyName} was already assigned once.");

        ExtractControlKey(hkKeys);

        KeyBind newHotkey = new();
        HotkeysUp.Add(hotkeyName, newHotkey);

        return newHotkey;
    }

    private void ExtractControlKey(Key[] hkKeys)
    {
        if (!ControlKeys.Contains(hkKeys[0]))
            ControlKeys.Add(hkKeys[0]);
    }

    public KeyBind OnKeyDown(Key key)
    {
        string keyName = key.ToString();

        if (KeyDownBinds.ContainsKey(keyName))
            throw new Exception($"Key down bind for {keyName} was already assigned once.");

        KeyBind keyDownBind = new();
        KeyDownBinds.Add(keyName, keyDownBind);

        return keyDownBind;
    }

    public KeyBind OnKeyUp(Key key)
    {
        string keyName = key.ToString();

        if (KeyUpBinds.ContainsKey(keyName))
            throw new Exception($"Key up bind for {keyName} was already assigned once.");

        KeyBind keyUpBind = new();
        KeyUpBinds.Add(keyName, keyUpBind);

        return keyUpBind;
    }

    public void HandleKeyDown(Key key)
    {
        KeySequence.Add(key);

        if (KeySequence.Count == 1 && ControlKeys.Contains(key))
        {
            IsHotkey = true;
            return;
        }

        if (TryHotkeyDown()) return;

        // Handle normal keydown
        string keyName = key.ToString();
        bool hasBind = KeyDownBinds.TryGetValue(keyName, out KeyBind? keyDownEvent);

        if (!IsHotkey && hasBind)
        {
            keyDownEvent?.Method?.Invoke();
            Console.WriteLine($"Key down bind: {key}");
        }
    }

    public void HandleKeyUp(Key key)
    {
        if (IsHotkey)
        {
            string hotkeyIndex = GetHotkeyName(KeySequence.ToArray());

            if (HotkeysUp.ContainsKey(hotkeyIndex))
            {
                HotkeysUp[hotkeyIndex].Method?.Invoke();

                Console.WriteLine($"Hotkey up bind: {hotkeyIndex}");
            }
        }

        KeySequence.Remove(key);

        TryHotkeyDown();

        if (KeySequence.Count == 0)
        {
            IsHotkey = false;
        }

        string keyName = key.ToString();
        bool hasBind = KeyUpBinds.TryGetValue(keyName, out KeyBind? keyUpEvent);

        if (!IsHotkey && hasBind)
        {
            bool isUpRestricted = HasUpRestriction(key);

            if (!isUpRestricted)
            {
                keyUpEvent?.Method?.Invoke();
                Console.WriteLine($"Key up bind: {key}");
            }
            else
            {
                Console.WriteLine($"Key up [!Restricted!]: {key}");
            }
        }
    }

    private bool HasUpRestriction(Key key)
    {
        string keyName = key.ToString();
        bool hasRestriction = KeyUpRestrictions.TryGetValue(keyName, out KeyUpRestriction? restriction);

        if (hasRestriction && restriction != null)
            return restriction.Keys.Any(k => KeySequence.Contains(k));

        return false;
    }


    private bool TryHotkeyDown()
    {
        if (IsHotkey)
        {
            string hotkeyIndex = GetHotkeyName(KeySequence.ToArray());

            if (HotkeysDown.ContainsKey(hotkeyIndex))
            {
                HotkeysDown[hotkeyIndex].Method?.Invoke();

                Console.WriteLine($"Hotkey down bind: {hotkeyIndex}");
                return true;
            }
        }

        return false;
    }

    public void HandleKeyChar(char ch) =>
        OnKeyType?.Invoke(ch);

    private static void MapKeys<T>(Array arr, out T[] target)
    {
        T[] tg = new T[arr.Length];

        for (int i = 0; i < arr.Length; i++)
            tg[i] = (T)arr.GetValue(i);

        target = tg;
    }

    private string GetHotkeyName(Key[] hkKeys)
    {
        Array.Sort(hkKeys);
        return string.Join(':', hkKeys);
    }
}

public class KeyBind
{
    public Action Method = default!;

    public void Perform(Action action)
    {
        Method = action;
    }
}

public class KeyUpRestriction
{
    public readonly Key[] Keys = default!;

    public KeyUpRestriction(Key[] keys)
    {
        Keys = keys;
    }
}