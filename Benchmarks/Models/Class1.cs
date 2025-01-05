namespace Benchmarks.Models;

public interface Interface1;
public interface Interface2;
public interface Interface3;
public interface Interface4;
public interface Interface5;

public class Class1 : Interface1;
public class Class2(Class1 class1) : Interface2;
public class Class3(Class2 class2) : Interface3;
public class Class4(Class3 class3) : Interface4;
public class Class5(Class4 class4) : Interface5;