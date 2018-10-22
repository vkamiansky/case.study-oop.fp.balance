# ООП и ФП -> поиск баланса

13.10.2018 на конференции SECR в Москве и 16.10.2018 в рамках семинара First Line Software в Санкт-Петербурге был прочитан доклад по теме "ООП и ФП в мейнстримовом программировании: ищем идеальный баланс с C# и F#". Конечно же, баланс, о котором идёт речь в докладе,  лежит в плоскости проектирования и инженерии на стыке двух парадигм. Осознание дуальности и пересечения идей, техник ООП и ФП даёт возможность прийти к более глубокому их пониманию, а значит и к эффективному их применению на практике. Ещё Роберт Кирхгоф говорил, что нет ничего практичнее хорошей теории. Здесь вы сможете найти материалы к докладу, в том числе развёрнутые ответы на наиболее острые вопросы аудитории, использовать код из демо-части, как лабораторию для проверки своих идей. Кроме того, раздел Issues - отличный способ поделиться своими ценными мыслями.

 - [__Презентация к докладу в формате PPTX__](https://raw.github.com/wiki/vkamiansky/case.study-oop.fp.balance/oop-fp-finding-balance.pptx)
 
 - [__Видео с семинара в First Line Software__](https://yadi.sk/i/JbDVA8Zkj1lvJA)
 
## Острый вопрос: Декларативный подход с чистым ООП – должно быть возможно! (или без ФП нельзя?)

### Что мы вообще хотим от декларативного программирования? 

Какую проблему мы решаем (sic!)?

В своей статье ["Серебряной пули нет" (англ. "No Silver Bullet – Essence and Accident in Software Engineering")](http://faculty.salisbury.edu/~xswang/Research/Papers/SERelated/no-silver-bullet.pdf) Фредерик Брукс разделяет сложности в создании программных решений на две категории:

__*Имманентные сложности*__ *(essential complexity)* – сложности, присущие решаемой задаче. 

__*Ненужные сложности*__ *(accidental complexity)* – сложности, присущие выбранному способу решения задачи.

Имманентные сложности никуда не денутся: если их нет – решение не нужно вообще. (см. выше: "какую проблему мы решаем?")

Декларативное программирование – решение проблемы ненужных сложностей в коде.

### А как декларативное программирование эту задачу решает?

Создаются предметно-ориентированные языки [DSL](https://en.wikipedia.org/wiki/Domain-specific_language), которые позволяют описывать задачу, а не её решение.

Можно сделать __внешний DSL__, как SQL (с нуля) или как XAML (на базе существующего DSL).

А можно разработать __внутренний DSL__ в форме API для языка общего назначения (как LINQ в C#).

### Заголовок статьи про внутренний DSL, не так ли?

Да. С внешними DSL нас скорее интересует валидация, трансляция. Для внутренних DSL актуален вопрос о синтаксисе и парадигме языка-хозяина.

### Для DSL язык-хозяин должен поддерживать ФП?

Рассмотрим пример с простейшим DSL для работы с потоками данных(`Stream`). В тексте программы ниже про `Stream` ни слова – описываем задачу, а не решение.
```c#
class Program
{
    static void Main(string[] args)
    {
        var (success, exception) = DataFunctions.Copy()
            .FromFile("try.json")
            .ToFile("try2.json");
    }
}
```
Под капотом, конечно, всё сложнее. Создаётся и возвращается в форме `Action<Stream, Stream>` алгоритм переноса данных из входного потока в выходной. Этот алгоритм используется для создания нового алгоритма, выполняющего перенос данных из файла в выходной поток. В итоге, алгоритм запускается, а в качестве выходного потока используется поток записи в файл.

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
2. Нам хочется конструировать и применять стратегии, используя оператор обращения к члену (точку), но
   - нельзя нарушать инкапсуляцию (никаких функций в чистом ООП),
   - конкретные переходы между типами должны быть вынесены за пределы типов. Их можно также абстрагировать в виде стратегий для расширяемости языка,
   - нужен контроль допустимости переходов между типами на этапе компиляции.

   Мы создадим интерфейсы для стратегии переходов между типами (переход может выражаться в декорировании, адаптации или, в общем случае, в вычислении нового объекта на базе старого), и для стратегий работы с потоками.

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

- возможность добавлять в язык типовые переходы `Transition` (слова) для алгоритмов переноса, записи данных, запуска алгоритмов (здесь это `Transition.WriteFile("try2.json")`) без изменения реализации существующих слов,
- проверка допустимости переходов на этапе компиляции (попытка передать переход из неподходящего типа `Strategy.Copy()
.Transit(Transition.WriteFile("try2.json"))` приведёт к ошибке компилятора),
- нотация с точкой работает и позволяет конструировать и выполнять алгоритм, как одно выражение.

Чего добиться не удалось?

- Из-за ограничения инкапсуляции в ООП мы не можем использовать переходы между типами напрямую в стиле `DataFunctions.Copy().FromFile("try.json").ToFile("try2.json")`, иначе, нам придётся реализовывать конкретные методы перехода (например,`ToFile(string path)`) в форме членов классов-источников перехода и пожертвовать расширяемостью нашего DSL, то есть главной целью его проектирования.

### Что в итоге?

 - Фундаментальные требования ООП в части инкапсуляции не позволяют скопировать элегантность языка, созданного нами с помощью средств ФП. Приходится делать выбор между расширяемостью языка и удобством его API для пользователя.

 - В ООП варианте очень много кода, относящегося исключительно к решению задачи проектирования. Для пользователя случайная сложность, благодаря нашему языку, может быть снижена, но на уровне реализации она остаётся высокой. Восприятие кода затруднено.

 - При проектировании языка мы использовали достаточно редкие инженерные решения (стратегия преобразования типа). Трудно прийти к такому решению, если сознательно не пытаться сымитировать подход из парадигмы ФП. Проектировочное решение сложно формулируется.

В итоге, ООП-реализация DSL требует значительных усилий при проектировании и не даёт того уровня удобства для пользователя, который бы оправдал такие усилия.

С другой стороны, внедрение в ООП-язык ФП элементов делает создание элегантных DSL удобным и естественным.

Взрывной рост процента популярных C# библиотек, использующих DSL как API, после прихода ФП в C# в 2007 (LINQ, Entity Framework, Moq) можно объяснить именно снятием ограничений, накладываемых практикой чистого ООП.

При наличии в C# необходимых элементов ФП DSL смог стать в этом языке ведущим паттерном проектирования API библиотек.

## Вопрос: Как удобно визуализировать свой DSL?

В этом репозитории представлен внутренний DSL на базе C# для обработки данных. На нём могут быть записаны выражения, задающие процесс обработки данных. Разумеется, нам хотелось бы визуализировать всё множество возможных последовательностей слов из нашего языка, которые будут давать валидные выражения. Говоря иначе, описать синтаксис нашего языка. Валидность синтаксиса с точки зрения C# нас в данном случае не очень интересует, так как её за нас проверит среда разработки. Для отображения всех возможных выражений языка в минимальном достаточном псевдо-коде нам прекрасно подойдёт формат [__синтаксических (железнодорожных)__ диаграмм (railroad diagram)](https://en.wikipedia.org/wiki/Syntax_diagram). 

Читать такие диаграммы очень просто. Любая последовательность, для которой в нашей диаграмме есть маршрут, будет валидной, все остальные - нет. В овалах здесь изображаются слова из нашего языка, а в прямоугольниках выражения для которых есть свои диаграммы.  Железнодорожная диаграмма для выражения операции (`DataProcessingOperation`) на нашем DSL обработки данных представлена ниже.

__DataProcessingOperation:__

![Железнодорожная диаграмма для языка из примера](https://raw.github.com/wiki/vkamiansky/case.study-oop.fp.balance/DataProcessingOperationRailroad.svg?sanitize=true)

Задаются ЖД диаграммы при помощи [расширенной формы Бэкуса-Наура (extended Backus-Naur form)](https://ru.wikipedia.org/wiki/Расширенная_форма_Бэкуса_—_Наура), что звучит устрашающе, но на деле совсем не сложно. Ниже приведён пример для нашего случая и, сопоставляя диаграмму и запись в EBNF, нетрудно уловить закономерность.

```ebnf
/* Example data processing DSL pseudo-code syntax notation in extended Backus-Naur form. */

DataProcessingOperation
         ::= ( 'DataFunctions' ( '.Copy' | '.ReadJsonArray' '.Map'? '.WriteJson' ) ( '.FromFile' | '.FromString' ) | EnumerableSequence '.ToJson' ) ( '.ToZipPart' '.ToZip' )* '.ToFile'
```

Диаграмму как эта можно построить при помощи сервиса [Railroad Diagram Generator](http://www.bottlecaps.de/rr/ui). Он даже позволяет выбрать свой любимый цвет (здесь #292d32).

В итоге, мы получили простую и наглядную визуализацию языка. Однако, следует заметить, что железнодорожные диаграммы дают хорошее представление о языке с точки зрения пользователя. Для визуализации внутреннего устройства языка может быть более полезен язык [теории категорий](https://ru.wikipedia.org/wiki/Теория_категорий).
