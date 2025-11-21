// File: Program.cs
using System;
using System.IO;
using Sample;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== MonitorUpdaterManagerSample - Prueba ===");

        var tempBase = Path.Combine(Path.GetTempPath(), "monitorUpdaterTest");
        var source = Path.Combine(tempBase, "source");
        var target = Path.Combine(tempBase, "target");

        // Limpiar pruebas previas
        if (Directory.Exists(tempBase)) Directory.Delete(tempBase, true);

        Directory.CreateDirectory(source);
        Directory.CreateDirectory(target);

        // Simular archivos de actualización:
        // 1) Un archivo nuevo (archivo.add)
        File.WriteAllText(Path.Combine(source, "hola.txt.add"), "contenido nuevo");

        // 2) Un archivo para actualizar (archivo.upd)
        var existingFilePath = Path.Combine(target, "existente.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(existingFilePath));
        File.WriteAllText(existingFilePath, "contenido viejo");
        File.WriteAllText(Path.Combine(source, "existente.txt.upd"), "contenido actualizado");

        // 3) Archivo para borrar (archivo.del)
        var toDelete = Path.Combine(target, "aEliminar.txt");
        File.WriteAllText(toDelete, "borrar esto");
        File.WriteAllText(Path.Combine(source, "aEliminar.txt.del"), string.Empty);

        // Llamar al updater
        MonitorUpdaterManagerSample.UpdateMonitor(source, target, "1.0.0");

        Console.WriteLine("=== Fin de la prueba. Compruebe la carpeta target: " + target);
        Console.WriteLine("Archivos en target:");
        foreach (var f in Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories))
        {
            Console.WriteLine(" - " + f + " (contenido: " + File.ReadAllText(f) + ")");
        }

        Console.WriteLine("Pulsa Enter para salir...");
        Console.ReadLine();
    }
}
