using Moq;
using Order_Project.Models;
using Order_Project.Services;
using Order_Project.Services.Intefraces;

namespace Order_Project_Tests
{
    public class OrderServiceTests
    {
        private readonly Mock<IInventoryService> _inventoryMock;
        private readonly Mock<IPaymentService> _paymentMock;
        private readonly Mock<INotificationService> _notificationMock;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            _inventoryMock = new Mock<IInventoryService>();
            _paymentMock = new Mock<IPaymentService>();
            _notificationMock = new Mock<INotificationService>();

            _service = new OrderService(_inventoryMock.Object, _paymentMock.Object, _notificationMock.Object);
        }
        // 1. Успішне створення замовлення
    /// <summary>
    /// Перевіряє, що замовлення створюється успішно при правильних даних.
    /// Перевіряє, що об’єкт не null, властивості відповідають введеним даним, 
    /// оплата пройшла успішно та виклик підтвердження відправлений один раз.
    /// </summary>
        [Fact]
        public void CreateOrder_ValidOrder_ReturnsOrder()
        {
            string product = "Book";
            int quantity = 2;
            _inventoryMock.Setup(i => i.CheckStock(product, quantity)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(true);

            var order = _service.CreateOrder(product, quantity);

            Assert.NotNull(order);
            Assert.Equal(product, order.Product);
            Assert.Equal(quantity, order.Quantity);
            Assert.True(order.IsPaid);
            _notificationMock.Verify(n => n.SendConfirmation(order), Times.Once);
        }

        // 2. Створення замовлення, коли немає товару
    /// <summary>
    /// Перевіряє, що створення замовлення не вдається, якщо на складі недостатньо товару.
    /// Має згенерувати виняток InvalidOperationException.
    /// </summary>
        [Fact]
        public void CreateOrder_NotEnoughStock_ThrowsException()
        {
            _inventoryMock.Setup(i => i.CheckStock(It.IsAny<string>(), It.IsAny<int>())).Returns(false);
            Assert.Throws<InvalidOperationException>(() => _service.CreateOrder("Book", 5));
        }

        // 3. Створення замовлення з негативною кількістю
    /// <summary>
    /// Параметризований тест, який перевіряє створення замовлення для різних комбінацій продуктів і кількостей.
    /// Використовує [Theory] та [InlineData].
    /// </summary>
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void CreateOrder_InvalidQuantity_ThrowsException(int quantity)
        {
            Assert.Throws<ArgumentException>(() => _service.CreateOrder("Book", quantity));
        }

        // 4. Успішне оновлення замовлення
    /// <summary>
    /// Перевіряє успішне оновлення кількості існуючого замовлення.
    /// </summary>
        [Fact]
        public void UpdateOrder_Valid_ReturnsTrue()
        {
            _inventoryMock.Setup(i => i.CheckStock("Book", 1)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(true);
            var order = _service.CreateOrder("Book", 1);

            var result = _service.UpdateOrder(order.Id, 5);

            Assert.True(result);
            Assert.Equal(5, order.Quantity);
        }

        // 5. Оновлення неіснуючого замовлення
    /// <summary>
    /// Перевіряє, що оновлення неіснуючого замовлення повертає false.
    /// </summary>
        [Fact]
        public void UpdateOrder_NonExistent_ReturnsFalse()
        {
            var result = _service.UpdateOrder(999, 5);
            Assert.False(result);
        }

        // 6. Видалення замовлення успішне
    /// <summary>
    /// Перевіряє успішне видалення існуючого замовлення
    /// та виклик збільшення складу.
    /// </summary>
        [Fact]
        public void RemoveOrder_Valid_ReturnsTrue()
        {
            _inventoryMock.Setup(i => i.CheckStock("Book", 1)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(true);
            var order = _service.CreateOrder("Book", 1);

            var result = _service.RemoveOrder(order.Id);

            Assert.True(result);
            _inventoryMock.Verify(i => i.IncreaseStock("Book", 1), Times.Once);
        }

        // 7. Видалення неіснуючого замовлення
    /// <summary>
    /// Перевіряє, що видалення неіснуючого замовлення повертає false.
    /// </summary>
        [Fact]
        public void RemoveOrder_NonExistent_ReturnsFalse()
        {
            var result = _service.RemoveOrder(999);
            Assert.False(result);
        }

        // 8. Список замовлень не порожній після створення
    /// <summary>
    /// Перевіряє, що список замовлень не порожній після створення одного замовлення.
    /// </summary>
        [Fact]
        public void GetOrders_NotEmptyAfterCreate()
        {
            _inventoryMock.Setup(i => i.CheckStock("Book", 1)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(true);
            _service.CreateOrder("Book", 1);

            var orders = _service.GetOrders();

            Assert.NotEmpty(orders);
        }

        // 9. Список замовлень порожній на старті
    /// <summary>
    /// Перевіряє, що на початку список замовлень порожній.
    /// </summary>
        [Fact]
        public void GetOrders_EmptyInitially()
        {
            var orders = _service.GetOrders();
            Assert.Empty(orders);
        }

        // 10. Платіж неуспішний
    /// <summary>
    /// Перевіряє, що при невдалій оплаті замовлення згенерується InvalidOperationException,
    /// склад не зміниться та підтвердження не відправиться.
    /// </summary>
        [Fact]
        public void CreateOrder_PaymentFails_ThrowsException()
        {
            _inventoryMock.Setup(i => i.CheckStock("Book", 1)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(false);

            Assert.Throws<InvalidOperationException>(() => _service.CreateOrder("Book", 1));
            _inventoryMock.Verify(i => i.IncreaseStock("Book", 1), Times.Once);
            _notificationMock.Verify(n => n.SendConfirmation(It.IsAny<Order>()), Times.Never);
        }

        // 11. Параметризоване створення замовлення
    /// <summary>
    /// Параметризований тест для створення замовлень з різними комбінаціями продуктів і кількостей.
    /// </summary>
        [Theory]
        [InlineData("Book", 1)]
        [InlineData("Pen", 3)]
        [InlineData("Notebook", 5)]
        public void CreateOrder_TheoryTest(string product, int quantity)
        {
            _inventoryMock.Setup(i => i.CheckStock(product, quantity)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(true);

            var order = _service.CreateOrder(product, quantity);

            Assert.Equal(product, order.Product);
            Assert.Equal(quantity, order.Quantity);
        }

        // 12. Перевірка наявності замовлення у списку
    /// <summary>
    /// Перевіряє, що створене замовлення присутнє у списку замовлень.
    /// </summary>
        [Fact]
        public void GetOrders_ContainsOrder()
        {
            _inventoryMock.Setup(i => i.CheckStock("Book", 1)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(true);
            var order = _service.CreateOrder("Book", 1);

            var orders = _service.GetOrders();
            Assert.Contains(order, orders);
        }

        // 13. Виклик ProcessPayment з будь-яким замовленням
    /// <summary>
    /// Перевіряє, що метод ProcessPayment викликається для будь-якого замовлення.
    /// </summary>
        [Fact]
        public void PaymentService_CalledWithAnyOrder()
        {
            _inventoryMock.Setup(i => i.CheckStock("Book", 1)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(true);

            var order = _service.CreateOrder("Book", 1);

            _paymentMock.Verify(p => p.ProcessPayment(It.IsAny<Order>()), Times.Once);
        }

        // 14. Виклик ProcessPayment з умовою
    /// <summary>
    /// Перевіряє, що метод ProcessPayment викликається з замовленням, що відповідає певній умові.
    /// </summary>
        [Fact]
        public void PaymentService_CalledWithCondition()
        {
            _inventoryMock.Setup(i => i.CheckStock("Book", 10)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(true);

            var order = _service.CreateOrder("Book", 10);

            _paymentMock.Verify(p => p.ProcessPayment(It.Is<Order>(o => o.Quantity == 10)), Times.Once);
        }

        // 15. Максимальна кількість викликів
    /// <summary>
    /// Перевіряє, що метод SendConfirmation викликається максимум один раз для замовлення.
    /// </summary>
        [Fact]
        public void NotificationService_CalledAtMostOnce()
        {
            _inventoryMock.Setup(i => i.CheckStock("Book", 1)).Returns(true);
            _paymentMock.Setup(p => p.ProcessPayment(It.IsAny<Order>())).Returns(true);

            var order = _service.CreateOrder("Book", 1);

            _notificationMock.Verify(n => n.SendConfirmation(order), Times.AtMost(1));
        }
    }

}
