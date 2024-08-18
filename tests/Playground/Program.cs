using Symptum.Core.Math;
using System.Diagnostics;

namespace Playground;

//public class Test
//{
//    const char ExtensionSeparator = '.';
//    const char PathSeparator = '\\';
//    string p1 = "\\Folder1\\Folder2\\Folder3\\Folder4\\Folder5\\FileName.ext";
//    //string p2 = "\\Folder1\\Folder2\\Folder3\\Folder4\\Folder5\\FileName2.ext2";

//    [Benchmark]
//    public void T1() => GetDetailsFromFilePath(p1);

//    [Benchmark]
//    public void T2() => GetDetailsFromFilePath1(p1);

//    [Benchmark]
//    public void T3() => GetDetailsFromFilePath2(p1);

//    public static (string folder, string fileName, string extension) GetDetailsFromFilePath(string? filePath)
//    {
//        string folder = string.Empty;
//        string fileName = string.Empty;
//        string extension = string.Empty;

//        if (filePath == null) return (folder, fileName, extension);

//        int dotIndex, slashIndex;
//        dotIndex = slashIndex = filePath.Length;

//        for (int i = filePath.Length - 1; i >= 0; i--)
//        {
//            char ch = filePath[i];
//            if (ch == PathSeparator)
//            {
//                slashIndex = i + 1; // To include '\'
//                break;
//            }
//            else if (ch == ExtensionSeparator)
//            {
//                dotIndex = i;
//                continue;
//            }
//        }

//        if (dotIndex > 0 && slashIndex > 0)
//        {
//            folder = filePath[..slashIndex];
//            fileName = filePath[slashIndex..dotIndex];
//            extension = filePath[dotIndex..];
//        }

//        return (folder, fileName, extension);
//    }

//    public static (string folder, string fileName, string extension) GetDetailsFromFilePath1(string? filePath)
//    {
//        string folder = string.Empty;
//        string fileName = string.Empty;
//        string extension = string.Empty;

//        if (filePath == null) return (folder, fileName, extension);

//        int dotIndex = 0, slashIndex = 0;
//        for (int i = 0; i < filePath?.Length; i++)
//        {
//            if (filePath[i] == PathSeparator)
//                slashIndex = i;
//            else if (filePath[i] == ExtensionSeparator)
//                dotIndex = i;
//        }

//        folder = filePath[..slashIndex];
//        fileName = filePath[slashIndex..dotIndex];
//        extension = filePath[dotIndex..];

//        return (folder, fileName, extension);
//    }

//    public static (string folder, string fileName, string extension) GetDetailsFromFilePath2(string? filePath)
//    {
//        string folder = string.Empty;
//        string fileName = string.Empty;
//        string extension = string.Empty;

//        if (filePath == null) return (folder, fileName, extension);

//        var l1 = filePath.Split(ExtensionSeparator);
//        if (l1.Length > 1)
//        {
//            extension = ExtensionSeparator + l1[l1.Length - 1];
//        }

//        var l2 = filePath.Split(PathSeparator);
//        if (l2.Length > 1)
//        {
//            fileName = l2[l2.Length - 1];
//            fileName = fileName.Remove(fileName.Length - extension.Length, extension.Length);
//        }

//        folder = filePath.Remove(filePath.Length - fileName.Length - extension.Length, fileName.Length + extension.Length);

//        return (folder, fileName, extension);
//    }
//}

//public class Test2
//{
//    string lastId = "Subjects.99.QBank99.Paper99-99.Topic99-99-99";
//    Uri lastUri = new("symptum://subjects/99/qbank99/paper99-99/topic99-99-99");

//    [GlobalSetup]
//    public void LoadSubjects()
//    {
//        for (int i = 0; i < 100; i++)
//        {
//            Subject sub = new()
//            {
//                Title = "Subject " + i,
//                Id = "Subjects." + i,
//                Uri = ResourceManager.GetAbsoluteUri("subjects/" + i),
//            };
//            QuestionBank qb = new()
//            {
//                Title = "QBank " + i,
//                Id = sub?.Id + ".QBank" + i,
//                Uri = new(sub?.Uri?.ToString() + "/qbank" + i),
//                Papers = [],
//                SplitMetadata = true
//            };
//            sub.QuestionBank = qb;
//            for (int j = 0; j < 100; j++)
//            {
//                QuestionBankPaper paper = new()
//                {
//                    Title = $"Paper {i}-{j}",
//                    Id = qb?.Id + $".Paper{i}-{j}",
//                    Uri = new(qb?.Uri?.ToString() + $"/paper{i}-{j}"),
//                    Topics = [],
//                    SplitMetadata = int.IsEvenInteger(j)
//                };
//                qb.Papers.Add(paper);

//                for (int k = 0; k < 100; k++)
//                {
//                    QuestionBankTopic topic = new()
//                    {
//                        Title = $"Topic {i}-{j}-{k}",
//                        Id = paper?.Id + $".Topic{i}-{j}-{k}",
//                        Uri = new(paper?.Uri?.ToString() + $"/topic{i}-{j}-{k}")
//                    };
//                    paper.Topics.Add(topic);
//                }
//            }

//            ResourceManager.Resources.Add(sub);
//            if (sub is IResource res)
//            {
//                res.InitializeResource(null);
//                res.ActivateResource();

//                foreach (var child in res.ChildrenResources)
//                {
//                    child.ActivateResource();
//                    foreach (var child1 in child.ChildrenResources)
//                    {
//                        child1.ActivateResource();
//                    }
//                }
//            }
//        }
//    }

//    [Benchmark]
//    public void T1() => ResourceManager.TryGetResourceFromId(lastId, out _);

//    [Benchmark]
//    public void T2() => ResourceManager.TryGetResourceFromUri(lastUri, out _);
//}

public class Program
{
    static string v1 = "[-10, 10]";
    static NumericalValue nv1 = new()
    {
        IsInterval = true,
        Minimum = -10,
        IncludesMinimum = true,
        Maximum = 10,
        IncludesMaximum = true
    };
    static string v2 = "(-5.005, 5.005)";
    static NumericalValue nv2 = new()
    {
        IsInterval = true,
        Minimum = -5.005,
        IncludesMinimum = false,
        Maximum = 5.005,
        IncludesMaximum = false
    };
    static string v3 = "[-3, 2)";
    static NumericalValue nv3 = new()
    {
        IsInterval = true,
        Minimum = -3,
        IncludesMinimum = true,
        Maximum = 2,
        IncludesMaximum = false
    };
    static string v4 = "(-200, 100]";
    static NumericalValue nv4 = new()
    {
        IsInterval = true,
        Minimum = -200,
        IncludesMinimum = false,
        Maximum = 100,
        IncludesMaximum = true
    };
    static string v5 = "[-10, _)";
    static NumericalValue nv5 = new()
    {
        IsInterval = true,
        Minimum = -10,
        IncludesMinimum = true,
        Maximum = double.PositiveInfinity,
        IncludesMaximum = false
    };
    static string v6 = "(_, 5.005]";
    static NumericalValue nv6 = new()
    {
        IsInterval = true,
        Minimum = double.NegativeInfinity,
        IncludesMinimum = false,
        Maximum = 5.005,
        IncludesMaximum = true
    };
    static string v7 = "(-10, _)";
    static NumericalValue nv7 = new()
    {
        IsInterval = true,
        Minimum = -10,
        IncludesMinimum = false,
        Maximum = double.PositiveInfinity,
        IncludesMaximum = false
    };
    static string v8 = "(_, 5.005)";
    static NumericalValue nv8 = new()
    {
        IsInterval = true,
        Minimum = double.NegativeInfinity,
        IncludesMinimum = false,
        Maximum = 5.005,
        IncludesMaximum = false
    };
    static string v9 = "pm(-5, 10)";
    static NumericalValue nv9 = new()
    {
        IsErrorInterval = true,
        Value = -5,
        Error = 10
    };
    static string v10 = "1.234";
    static NumericalValue nv10 = new()
    {
        Value = 1.234,
        IsInterval = false,
        Minimum = double.NaN,
        IncludesMinimum = false,
        Maximum = double.NaN,
        IncludesMaximum = false
    };

    public static void Main(string[] args)
    {
        if (NumericalValue.TryParse(v1, out var value))
        {
            Debug.Assert(nv1 == value);
        }
        if (NumericalValue.TryParse(v2, out value))
        {
            Debug.Assert(nv2 == value);
        }
        if (NumericalValue.TryParse(v3, out value))
        {
            Debug.Assert(nv3 == value);
        }
        if (NumericalValue.TryParse(v4, out value))
        {
            Debug.Assert(nv4 == value);
        }
        if (NumericalValue.TryParse(v5, out value))
        {
            Debug.Assert(nv5 == value);
        }
        if (NumericalValue.TryParse(v6, out value))
        {
            Debug.Assert(nv6 == value);
        }
        if (NumericalValue.TryParse(v7, out value))
        {
            Debug.Assert(nv7 == value);
        }
        if (NumericalValue.TryParse(v8, out value))
        {
            Debug.Assert(nv8 == value);
        }
        if (NumericalValue.TryParse(v9, out value))
        {
            Debug.Assert(nv9 == value);
        }
        if (NumericalValue.TryParse(v10, out value))
        {
            Debug.Assert(nv10 == value);
        }
        Debug.Assert(nv10.Equals(1.234));
        Debug.Assert(nv1.Contains(1.234));
        Debug.Assert(nv1.Contains(10));
        Debug.Assert(nv1.Contains(-10));
        Debug.Assert(nv2.Contains(-6)); // fail
        Debug.Assert(nv2.Contains(6)); // fail
        Debug.Assert(nv3.Contains(2)); // fail
        Debug.Assert(nv3.Contains(-3));
        Debug.Assert(nv4.Contains(-200)); // fail
        Debug.Assert(nv4.Contains(100));
        Debug.Assert(nv5.Contains(double.NegativeInfinity)); // fail
        Debug.Assert(nv5.Contains(1000000000));
        Debug.Assert(nv5.Contains(double.PositiveInfinity)); // fail
        Debug.Assert(nv6.Contains(double.NegativeInfinity)); // fail
        Debug.Assert(nv6.Contains(-100000000));
        Debug.Assert(nv6.Contains(double.PositiveInfinity)); // fail
        Debug.Assert(nv9.Contains(-15));
        Debug.Assert(nv9.Contains(5));
        Debug.Assert(nv9.Contains(-2));
        Debug.Assert(nv9.Contains(0));
        Debug.Assert(nv9.Contains(100)); // fail
        Debug.Assert(nv9.Contains(-100)); // fail

        Debug.Assert(nv1.ToString() == v1);
        Debug.Assert(nv2.ToString() == v2);
        Debug.Assert(nv3.ToString() == v3);
        Debug.Assert(nv4.ToString() == v4);
        Debug.Assert(nv5.ToString() == v5);
        Debug.Assert(nv6.ToString() == v6);
        Debug.Assert(nv7.ToString() == v7);
        Debug.Assert(nv8.ToString() == v8);
        Debug.Assert(nv9.ToString() == v9);
        Debug.Assert(nv10.ToString() == v10);
    }
}
