using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestComponent : SimpleECS.Component
{
    public int testValue
    {
        get => _testValue;
        set => UpdateProperty(ref _testValue, value);
    }

    private int _testValue;
}
