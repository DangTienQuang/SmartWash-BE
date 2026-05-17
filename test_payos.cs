using System;
using System.Linq;
using System.Reflection;

public class Test {
    public static void Main() {
        try {
            var asm = Assembly.Load("PayOS");
            Console.WriteLine("Assembly loaded: " + asm.FullName);
            foreach (var type in asm.GetTypes().Where(t => t.IsPublic)) {
                Console.WriteLine("Type: " + type.FullName);
            }
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
