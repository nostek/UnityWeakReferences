using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class UnityWeakReferenceType : Attribute
{
	public UnityWeakReferenceType(System.Type type)
	{
		this.Type = type;
	}

	public System.Type Type
	{
		private set;
		get;
	}
}
