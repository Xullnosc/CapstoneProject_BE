using AutoMapper;
using BusinessObjects.DTOs;

namespace FCTMS.Tests.Services
{
    public class ChecklistServiceTests
    {
        private readonly Mock<IChecklistRepository> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ChecklistService _service;

        public ChecklistServiceTests()
        {
            _mockRepository = new Mock<IChecklistRepository>();
            _mockMapper = new Mock<IMapper>();
            _service = new ChecklistService(_mockRepository.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedList()
        {
            var entities = new List<Checklist>
            {
                new Checklist { ChecklistId = 1, Title = "A", Content = "Content A" }
            };
            var dtos = new List<ChecklistDTO>
            {
                new ChecklistDTO { ChecklistId = 1, Title = "A", Content = "Content A" }
            };
            _mockRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(entities);
            _mockMapper.Setup(m => m.Map<List<ChecklistDTO>>(entities)).Returns(dtos);

            var result = await _service.GetAllAsync();

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("A");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            _mockRepository.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Checklist?)null);

            var result = await _service.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto_WhenFound()
        {
            var entity = new Checklist { ChecklistId = 1, Title = "T", Content = "C" };
            var dto = new ChecklistDTO { ChecklistId = 1, Title = "T", Content = "C" };
            _mockRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(entity);
            _mockMapper.Setup(m => m.Map<ChecklistDTO>(entity)).Returns(dto);

            var result = await _service.GetByIdAsync(1);

            result.Should().NotBeNull();
            result!.ChecklistId.Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreatedDto()
        {
            var dto = new ChecklistCreateDTO { Title = "New", Content = "New content" };
            var entity = new Checklist { ChecklistId = 10, Title = "New", Content = "New content" };
            var mappedDto = new ChecklistDTO { ChecklistId = 10, Title = "New", Content = "New content" };
            _mockMapper.Setup(m => m.Map<Checklist>(dto)).Returns(new Checklist { Title = dto.Title, Content = dto.Content });
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<Checklist>())).ReturnsAsync(entity);
            _mockMapper.Setup(m => m.Map<ChecklistDTO>(entity)).Returns(mappedDto);

            var result = await _service.CreateAsync(dto);

            result.Should().NotBeNull();
            result.ChecklistId.Should().Be(10);
            result.Title.Should().Be("New");
            _mockRepository.Verify(x => x.AddAsync(It.Is<Checklist>(e => e.Title == "New" && e.Content == "New content")), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateEntity_WhenFound()
        {
            int id = 1;
            var entity = new Checklist { ChecklistId = id, Title = "Old", Content = "Old" };
            var dto = new ChecklistUpdateDTO { Title = "Updated", Content = "Updated content" };
            _mockRepository.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);

            await _service.UpdateAsync(id, dto);

            entity.Title.Should().Be("Updated");
            entity.Content.Should().Be("Updated content");
            _mockRepository.Verify(x => x.UpdateAsync(entity), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            _mockRepository.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Checklist?)null);
            var dto = new ChecklistUpdateDTO { Title = "X", Content = "Y" };

            Func<Task> act = async () => await _service.UpdateAsync(999, dto);

            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Checklist with id 999 not found.");
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Checklist>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldCallRepository_WhenFound()
        {
            var entity = new Checklist { ChecklistId = 1 };
            _mockRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(entity);

            await _service.DeleteAsync(1);

            _mockRepository.Verify(x => x.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            _mockRepository.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Checklist?)null);

            Func<Task> act = async () => await _service.DeleteAsync(999);

            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Checklist with id 999 not found.");
            _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
