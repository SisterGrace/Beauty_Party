using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface)]
public class KeepNameAttribute : System.Attribute
{
    public string name;
    public KeepNameAttribute() { }
    public KeepNameAttribute(string name) { this.name = name; }
}