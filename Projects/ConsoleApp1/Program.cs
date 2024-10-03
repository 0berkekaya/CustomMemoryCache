
using NX.MemoryCacheLib;


A demoA = new()
{
    Id = 1,
    Name = "ATestName",
    Surname = "ATestSurname"
};
A demoAA = new()
{
    Id = 2,
    Name = "ATestName2",
    Surname = "ATestSurname2"
};

TSMemoryCacheManager<BaseClass> manager = new();
manager.Add("A", demoA);
manager.Add("A", demoAA);
manager.Add("B", new B());
manager.Add("C", new C());

manager.SafeDispose("A");

manager.Update("A", new A(), t => t is A a && a.Id == 1);
manager.Update("A",t=> t is A a && a.Id == 0, t =>
{
    t.Id = 999;
});
//manager.Remove("A",t=> t is A a && a.Name == demoA.Name);


Console.ReadLine();


class BaseClass
{
    public int Id { get; set; }
}

class A : BaseClass
{
    public string? Name { get; set; }
    public string? Surname { get; set; }
}

class B : BaseClass
{
    public string? GuidId { get; set; }
    public string? LastChacngeId { get; set; }
}

class C : BaseClass
{
    public int RelatedId { get; set; }
    public DateTime Date { get; set; }
}