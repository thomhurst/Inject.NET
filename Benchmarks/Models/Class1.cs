namespace Benchmarks.Models;

public interface Interface1;
public interface Interface2;
public interface Interface3;
public interface Interface4;
public interface Interface5;

public interface IGenericInterface<T>;

public class GenericClass<T>(T t) : IGenericInterface<T>;

public class GenericWrapper(IGenericInterface<Class1> generic);

public class Class1 : Interface1;
public class Class2(Interface1 @interface) : Interface2;
public class Class3(Interface2 @interface) : Interface3;
public class Class4(Interface3 @interface) : Interface4;
public class Class5(Interface4 @interface) : Interface5;