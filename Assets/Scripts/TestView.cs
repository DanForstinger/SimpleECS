using SimpleECS;
using TMPro;
using UnityEngine;

public class TestView : EntityView<TestComponent>
{
    [SerializeField] private TMP_Text text;

    public void Increment()
    {
        component.testValue++;
    }

    public void Decrement()
    {
        component.testValue--;
    }
    
    public override void OnComponentDestroy()
    {
        
    }

    public override void OnComponentUpdate(TestComponent newComp)
    {
        text.text = newComp.testValue.ToString();
    }

    protected override void OnComponentBind(TestComponent component, World world)
    {
        text.text = component.testValue.ToString();
    }
}
