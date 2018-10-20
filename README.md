# ООП и ФП -> поиск баланса

## Острый вопрос: ООП и декларативный подход - должно быть возможно!

### Что мы вообще хотим от декларативного программирования? 

Какую проблему мы решаем (sic!)?

В своей статье ["Серебряной пули нет" (англ. "No Silver Bullet - Essence and Accident in Software Engineering")](http://faculty.salisbury.edu/~xswang/Research/Papers/SERelated/no-silver-bullet.pdf) Фредерик Брукс разделяет сложности в создании программных решений на две категории:

__*Имманентные сложности*__ *(essential complexity)* - сложности, присущие решаемой задаче. 

__*Ненужные сложности*__ *(accidental complexity)* - сложности, присущие тому, как задача решается.

Имманентные сложности никуда не денутся: если их нет - решение не нужно вообще. 

Декларативное программирование - решение проблемы ненужных сложностей в коде.

### А как декларативное программирование эту задачу решает?

Создаются предметно-ориентированные языки [DSL](https://en.wikipedia.org/wiki/Domain-specific_language), которые позволяют описывать задачу, а не её решение.

Можно сделать __внешний DSL__, как SQL (с нуля) или как XAML (на базе существующего DSL).

А можно разработать __внутренний DSL__ в форме API для языка общего назначения (как LINQ в C#).

### Заголовок статьи про внутренний DSL, не так ли?

Да. С внешними DSL нас скорее интересует валидация, трансляция. Для внутренних DSL актуален вопрос о синтаксисе и парадигме языка-хозяина.

### Для DSL язык-хозяин должен поддерживать ФП?

Рассмотрим пример с простейшим DSL для работы с потоками данных(`Stream`). В тексте программы ниже про `Stream` не слова, описываем задачу, а не решение.
```c#
class Program
{
    static void Main(string[] args)
    {
        var (success, exception) =
    DataFunctions.Copy()
            .FromFile("try.json")
            .ToFile("try2.json");
    }
}
```
Под капотом, конечно, всё сложнее. Создаётся и возвращается в форме `Action<Stream, Stream>` алгоритм переноса данных из входного потока в выходной. Этот алгоритм используется для создания алгоритма переноса данных из файла в выходной поток. В итоге, алгоритм используется, а в качестве выходного потока используется поток записи в файл.

Звучит сложно, но реализация в C# совсем не так страшна, и оставляет мало места для случайных сложностей.
```c#
public static partial class DataFunctions
{
    public static Action<Stream, Stream> Copy()
    {
        return (inputStream, outputStream) => inputStream.CopyTo(outputStream);
    }

    public static Action<Stream> FromFile(this Action<Stream, Stream> transferData, string path)
    {
        return outputStream =>
        {
            using (var inputStream = File.Open(path, FileMode.Open))
                transferData(inputStream, outputStream);
        };
    }

    public static (bool success, Exception exception) ToFile(this Action<Stream> useOutputStream, string path)
    {
        try
        {
            using (var outputStream = File.Open(path, FileMode.Create))
                useOutputStream(outputStream);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }
}
```

Что потребовалось из ФП?

- Поддержка функций (методы расширения в C#);
- Функции как данные (анонимные делегаты в C#);
- Применение функций в форме "значение-оператор-функция" (нотация с точкой в C#).

### А теперь вопрос: тот же DSL, но без ФП.

ООП.
1. Абстрагируем алгоритмы записи в поток и переноса данных между потоками. Для этого используем паттерн [__стратегия__](https://ru.wikipedia.org/wiki/Стратегия_(шаблон_проектирования)).
2. Нам хочется конструировать и использовать стратегии, используя оператор обращения к члену, но
   - нельзя нарушать инкапсуляцию (никаких функций в чистом ООП),
   - нужно абстрагировать переходы переходы между типами, в виде стратегий для взаимозаменяемости,
   - нужно контроль на этапе компиляции допустимости переходов между типами.

   Мы создадим интерфейсы для стратегии перехода между типами (композиции или вычисления в общем случае), и для стратегий работы с потоками.

```c#
public interface ITypeTransition<TIn, TOut>
{
    TOut DoTransit(TIn source);
}

public interface ISupportTypeTransition<TIn>
{
    TOut Transit<TOut>(ITypeTransition<TIn, TOut> transition);
}

public interface IDataWritingStrategy : ISupportTypeTransition<IDataWritingStrategy>
{
    void Write(Stream outputStream);
}

public interface IDataRelayStrategy : ISupportTypeTransition<IDataRelayStrategy>
{
    void Relay(Stream inputStream, Stream outputStream);
}
```
Теперь осталось только создать конкретные реализации классов с такими интерфейсами. Причём, мы скорее всего не захотим их создавать через `new` или звать по имени, их конкретные имена для нас не важны.

```c#
public static class Strategy
{
    private class CopyStrategy : IDataRelayStrategy
    {
        public void Relay(Stream inputStream, Stream outputStream) => inputStream.CopyTo(outputStream);

        public TOut Transit<TOut>(ITypeTransition<IDataRelayStrategy, TOut> transition) => transition.DoTransit(this);
    }

    public static IDataRelayStrategy Copy() => new CopyStrategy();
}

public static class Transition
{
    private class FromFileTransition : ITypeTransition<IDataRelayStrategy, IDataWritingStrategy>
    {
        private class FromFileStrategy : IDataWritingStrategy
        {
            private IDataRelayStrategy _relayStrategy;
            private string _filePath;

            public FromFileStrategy(IDataRelayStrategy relayStrategy, string filePath)
            {
                _relayStrategy = relayStrategy;
                _filePath = filePath;
            }

            public void Write(Stream outputStream)
            {
                using (var inputStream = File.Open(_filePath, FileMode.Open))
                    _relayStrategy.Relay(inputStream, outputStream);
            }

            public TOut Transit<TOut>(ITypeTransition<IDataWritingStrategy, TOut> transition) => transition.DoTransit(this);
        }

        private string _filePath;

        public FromFileTransition(string filePath) => _filePath = filePath;

        public IDataWritingStrategy DoTransit(IDataRelayStrategy source) => new FromFileStrategy(source, _filePath);
    }

    private class WriteFileTransition : ITypeTransition<IDataWritingStrategy, Result>
    {
        private string _filePath;

        public WriteFileTransition(string filePath)
        {
            _filePath = filePath;
        }

        public Result DoTransit(IDataWritingStrategy source)
        {
            try
            {
                using (var outputStream = File.Open(_filePath, FileMode.Create))
                    source.Write(outputStream);

                return new Result(true, null);
            }
            catch (Exception ex)
            {
                return new Result(false, ex);
            }
        }
    }

    public static ITypeTransition<IDataRelayStrategy, IDataWritingStrategy> FromFile(string filePath) => new FromFileTransition(filePath);

    public static ITypeTransition<IDataWritingStrategy, Result> WriteFile(string filePath) => new WriteFileTransition(filePath);
}
```
Чтобы наши глаза немного отдохнули, посмотрим на реализацию класса `Result`.
```c#
public class Result
{
    public bool IsSuccess { get; private set; }
    public Exception Exception { get; private set; }

    public Result(bool isSuccess, Exception exception)
    {
        IsSuccess = isSuccess;
        Exception = exception;
    }
}
```
Итак, мы спроектировали систему типовых переходов, которая позволит строить DSL обработки данных с использованием чистого ООП. Посмотрим, что в итоге получает пользователь нашего API.
```c#
class Program
{
    static void Main(string[] args)
    {
        var result = Strategy.Copy()
            .Transit(Transition.FromFile("try.json"))
            .Transit(Transition.WriteFile("try2.json"));
    }
}
```
Чего мы смогли добиться?

- возможность добавлять переходы `Transition` (слова языка) для алгоритмов переноса, записи данных, различные способы запуска алгоритмов (здесь это `Transition.WriteFile("try2.json")`),
- проверка допустимости переходов на этапе компиляции (попытка передать переход из неподходящего типа `Strategy.Copy()
.Transit(Transition.WriteFile("try2.json"))` приведёт к ошибке компилятора),
- нотация с точкой работает и позволяет конструировать и выполнять алгоритм, как одно выражение.

Чего добиться не удалось?

- Из-за ограничения инкапсуляции в ООП мы не можем использовать переходы между типами напрямую в стиле `DataFunctions.Copy().FromFile("try.json").ToFile("try2.json")`, иначе, нам придётся реализовывать конкретные методы перехода (например,`ToFile(string path)`) в форме членов классов-источников перехода, а это разрушит гибкость проектируемого DSL.

### Что в итоге?

 - Фундаментальные требования ООП в части инкапсуляции не позволяют скопировать элегантность языка, созданного нами с помощью средств ФП. Страдает эргономика языка.

 - В ООП варианте очень много кода, относящегося исключительно к решению задачи проектирования. Для пользователя случайная сложность благодаря нашему языку может быть снижена, но на уровне реализации она остаётся высокой. Восприятие кода языка затруднено.

 - При проектировании языка мы использовали достаточно редкие инженерные решения (стратегия преобразования типа). Трудно прийти к такому решению, если сознательно не пытаться сымитировать подход из парадигмы ФП. Проектировочное решение сложно формулируется.

В итоге, ООП-реализация DSL требует значительных усилий при проектировании и не даёт того уровня удобства для пользователя, который бы оправдал эти усилия.

С другой стороны, внедрение в ООП-язык ФП элементов делает создание элегантных DSL удобным и естественным.

Взрывной рост процента популярных C# библиотек, использующих DSL как API после прихода ФП в C# в 2007 (LINQ, Entity Framework, Moq) можно объяснить именно снятием ограничений, накладываемых практикой чистого ООП.

При наличии в C# необходимых элементов ФП DSL смог стать в этом языке ведущим паттерном проектирования API библиотек.

