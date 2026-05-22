using attendance_monitoring.Exceptions;
using attendance_monitoring.Services.Crud;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ValidationException = attendance_monitoring.Exceptions.ValidationException;

namespace attendance.testproject.Services_Testing.Crud;

#region Test types

/// <summary>
/// Minimal POCO for CrudService tests.
/// </summary>
public class TestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateTestEntity
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateTestEntity
{
    public string? Name { get; set; }
}

#endregion

public class CrudServiceTests
{
    #region Helpers

    private const string EntityName = "TestEntity";

    private static readonly Guid TestId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static CrudServiceConfig<TestEntity, CreateTestEntity, UpdateTestEntity> BuildConfig(
        Func<DbUpdateException, (string FieldName, string FieldValue)?>? resolveUnique = null,
        Func<DbUpdateException, (string ConflictType, string Message)?>? resolveFk = null,
        IReadOnlyList<UpdateUniquenessCheck<TestEntity, UpdateTestEntity>>? updateChecks = null)
    {
        return new CrudServiceConfig<TestEntity, CreateTestEntity, UpdateTestEntity>
        {
            EntityName = EntityName,
            CreateUniquenessChecks =
            [
                new UniquenessCheck<CreateTestEntity>
                {
                    FieldName = "Name",
                    ValueSelector = dto => dto.Name,
                    ExistsAsync = _ => Task.FromResult(false)
                }
            ],
            UpdateUniquenessChecks = updateChecks ?? [],
            MapToEntity = dto => new TestEntity { Id = Guid.NewGuid(), Name = dto.Name },
            ApplyUpdate = (dto, entity) =>
            {
                if (dto.Name != null) entity.Name = dto.Name;
            },
            ResolveUniqueConstraintViolation = resolveUnique,
            ResolveForeignKeyViolation = resolveFk
        };
    }

    private static CrudServiceConfig<TestEntity, TestEntity, TestEntity> BuildSectionConfig(
        Func<DbUpdateException, (string ConflictType, string Message)?>? resolveFk = null)
    {
        return new CrudServiceConfig<TestEntity, TestEntity, TestEntity>
        {
            EntityName = "Section",
            CreateUniquenessChecks =
            [
                new UniquenessCheck<TestEntity>
                {
                    FieldName = "Name",
                    ValueSelector = e => e.Name,
                    ExistsAsync = _ => Task.FromResult(false)
                }
            ],
            UpdateUniquenessChecks = [],
            MapToEntity = dto => new TestEntity
            {
                Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
                Name = dto.Name
            },
            ApplyUpdate = (dto, entity) =>
            {
                if (dto.Name != null) entity.Name = dto.Name;
            },
            ResolveForeignKeyViolation = resolveFk
        };
    }

    private static Mock<IGenericCrudRepository<TestEntity>> CreateRepoMock()
    {
        return new Mock<IGenericCrudRepository<TestEntity>>();
    }

    private static Mock<ILogger<CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>>> CreateLoggerMock()
    {
        return new Mock<ILogger<CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>>>();
    }

    private static Mock<ILogger<CrudService<TestEntity, TestEntity, TestEntity>>> CreateSectionLoggerMock()
    {
        return new Mock<ILogger<CrudService<TestEntity, TestEntity, TestEntity>>>();
    }

    #endregion

    #region GetAll

    public class GetAll
    {
        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var entities = new List<TestEntity>
            {
                new() { Id = Guid.NewGuid(), Name = "A" },
                new() { Id = Guid.NewGuid(), Name = "B" }
            };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);

            // Act
            var result = (await service.GetAllAsync()).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Same(entities[0], result[0]);
            Assert.Same(entities[1], result[1]);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyList_WhenNoEntities()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<TestEntity>());

            // Act
            var result = (await service.GetAllAsync()).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_RepositoryThrows_ThrowsEntityServiceException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            repo.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("something went wrong"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityServiceException>(() => service.GetAllAsync());
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Contains("GetAll", ex.Operation);
            Assert.NotNull(ex.InnerException);
        }
    }

    #endregion

    #region GetById

    public class GetById
    {
        [Fact]
        public async Task GetByIdAsync_EntityExists_ReturnsEntity()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var entity = new TestEntity { Id = TestId, Name = "Found" };
            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync(entity);

            // Act
            var result = await service.GetByIdAsync(TestId);

            // Assert
            Assert.Same(entity, result);
        }

        [Fact]
        public async Task GetByIdAsync_EntityNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync((TestEntity?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => service.GetByIdAsync(TestId));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal(TestId, ex.Key);
        }

        [Fact]
        public async Task GetByIdAsync_RepositoryThrows_ThrowsEntityServiceException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            repo.Setup(r => r.GetByIdAsync(TestId)).ThrowsAsync(new Exception("db timeout"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityServiceException>(() => service.GetByIdAsync(TestId));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Contains("Get", ex.Operation);
            Assert.NotNull(ex.InnerException);
        }
    }

    #endregion

    #region Create

    public class Create
    {
        [Fact]
        public async Task CreateAsync_ValidInput_ReturnsCreatedEntity()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var dto = new CreateTestEntity { Name = "NewItem" };
            var created = new TestEntity { Id = Guid.NewGuid(), Name = "NewItem" };
            repo.Setup(r => r.CreateAsync(It.IsAny<TestEntity>())).ReturnsAsync(created);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.Same(created, result);
            repo.Verify(r => r.CreateAsync(It.Is<TestEntity>(e => e.Name == "NewItem")), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NullUniquenessValue_ThrowsValidationException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var dto = new CreateTestEntity { Name = null! };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
            Assert.Contains("Name", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhitespaceUniquenessValue_ThrowsValidationException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var dto = new CreateTestEntity { Name = "   " };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));
            Assert.Contains("Name", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_DuplicateNamePreCheck_ThrowsAlreadyExistsException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var uniquenessChecks = new List<UniquenessCheck<CreateTestEntity>>
            {
                new()
                {
                    FieldName = "Name",
                    ValueSelector = dto => dto.Name,
                    ExistsAsync = value => Task.FromResult(value == "Duplicate")
                }
            };
            var config = new CrudServiceConfig<TestEntity, CreateTestEntity, UpdateTestEntity>
            {
                EntityName = EntityName,
                CreateUniquenessChecks = uniquenessChecks,
                MapToEntity = dto => new TestEntity { Id = Guid.NewGuid(), Name = dto.Name },
                ApplyUpdate = (dto, entity) => { if (dto.Name != null) entity.Name = dto.Name; }
            };
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var dto = new CreateTestEntity { Name = "Duplicate" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => service.CreateAsync(dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal("Name", ex.IdentifierPropertyName);
            Assert.Equal("Duplicate", ex.EntityIdentifier);
        }

        [Fact]
        public async Task CreateAsync_UniqueConstraintViolation_ThrowsAlreadyExistsException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var innerException = new Exception("UNIQUE constraint failed: Test.Name");
            var dbUpdateException = new DbUpdateException("Test error", innerException);

            var config = BuildConfig(resolveUnique: ex => ("Name", "TakenName"));
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var dto = new CreateTestEntity { Name = "NewItem" };
            repo.Setup(r => r.CreateAsync(It.IsAny<TestEntity>())).ThrowsAsync(dbUpdateException);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => service.CreateAsync(dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal("Name", ex.IdentifierPropertyName);
            Assert.Equal("TakenName", ex.EntityIdentifier);
        }

        [Fact]
        public async Task CreateAsync_UniqueConstraintViolation_NullResolver_UsesDefaults()
        {
            // Arrange
            var repo = CreateRepoMock();
            var innerException = new Exception("UNIQUE constraint failed: Test.Name");
            var dbUpdateException = new DbUpdateException("Test error", innerException);

            var config = BuildConfig(resolveUnique: null);
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var dto = new CreateTestEntity { Name = "NewItem" };
            repo.Setup(r => r.CreateAsync(It.IsAny<TestEntity>())).ThrowsAsync(dbUpdateException);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => service.CreateAsync(dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal("field", ex.IdentifierPropertyName);
            Assert.Equal("value", ex.EntityIdentifier);
        }

        [Fact]
        public async Task CreateAsync_RepositoryThrows_ThrowsEntityServiceException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var dto = new CreateTestEntity { Name = "NewItem" };
            repo.Setup(r => r.CreateAsync(It.IsAny<TestEntity>())).ThrowsAsync(new Exception("unexpected error"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityServiceException>(() => service.CreateAsync(dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Contains("Create", ex.Operation);
            Assert.NotNull(ex.InnerException);
        }

        [Fact]
        public async Task CreateAsync_SaveChangesThrows_ThrowsEntityServiceException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var dto = new CreateTestEntity { Name = "NewItem" };
            var created = new TestEntity { Id = Guid.NewGuid(), Name = "NewItem" };
            repo.Setup(r => r.CreateAsync(It.IsAny<TestEntity>())).ReturnsAsync(created);
            repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new Exception("db connection lost"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityServiceException>(() => service.CreateAsync(dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Contains("Create", ex.Operation);
            Assert.NotNull(ex.InnerException);
        }
    }

    #endregion

    #region Update

    public class Update
    {
        [Fact]
        public async Task UpdateAsync_ValidInput_ReturnsUpdatedEntity()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "OldName" };
            repo.Setup(r => r.GetByIdTrackedAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var dto = new UpdateTestEntity { Name = "NewName" };

            // Act
            var result = await service.UpdateAsync(TestId, dto);

            // Assert
            Assert.Same(existing, result);
            Assert.Equal("NewName", result.Name);
            repo.Verify(r => r.Update(existing), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_EntityNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            repo.Setup(r => r.GetByIdTrackedAsync(TestId)).ReturnsAsync((TestEntity?)null);

            var dto = new UpdateTestEntity { Name = "NewName" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => service.UpdateAsync(TestId, dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal(TestId, ex.Key);
        }

        [Fact]
        public async Task UpdateAsync_DuplicateNameDifferentEntity_ThrowsAlreadyExistsException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var existing = new TestEntity { Id = TestId, Name = "Original" };
            var duplicate = new TestEntity { Id = Guid.NewGuid(), Name = "TakenName" };

            var updateChecks = new List<UpdateUniquenessCheck<TestEntity, UpdateTestEntity>>
            {
                new()
                {
                    FieldName = "Name",
                    ValueSelector = dto => dto.Name,
                    CurrentValueSelector = entity => entity.Name,
                    FindByUniqueFieldAsync = value => Task.FromResult(value == "TakenName" ? duplicate : null)
                }
            };
            var config = BuildConfig(updateChecks: updateChecks);
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            repo.Setup(r => r.GetByIdTrackedAsync(TestId)).ReturnsAsync(existing);

            var dto = new UpdateTestEntity { Name = "TakenName" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => service.UpdateAsync(TestId, dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal("Name", ex.IdentifierPropertyName);
            Assert.Equal("TakenName", ex.EntityIdentifier);
        }

        [Fact]
        public async Task UpdateAsync_RowsAffectedZero_ThrowsEntityServiceException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "OldName" };
            repo.Setup(r => r.GetByIdTrackedAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

            var dto = new UpdateTestEntity { Name = "NewName" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityServiceException>(() => service.UpdateAsync(TestId, dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Contains("Update", ex.Operation);
        }

        [Fact]
        public async Task UpdateAsync_UniqueConstraintViolation_ThrowsAlreadyExistsException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var innerException = new Exception("UNIQUE constraint failed: Test.Name");
            var dbUpdateException = new DbUpdateException("Test error", innerException);

            var config = BuildConfig(resolveUnique: ex => ("Name", "TakenName"));
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "OldName" };
            repo.Setup(r => r.GetByIdTrackedAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

            var dto = new UpdateTestEntity { Name = "NewName" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => service.UpdateAsync(TestId, dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal("Name", ex.IdentifierPropertyName);
            Assert.Equal("TakenName", ex.EntityIdentifier);
        }

        [Fact]
        public async Task UpdateAsync_RepositoryThrows_ThrowsEntityServiceException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "OldName" };
            repo.Setup(r => r.GetByIdTrackedAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new Exception("unexpected"));

            var dto = new UpdateTestEntity { Name = "NewName" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityServiceException>(() => service.UpdateAsync(TestId, dto));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Contains("Update", ex.Operation);
            Assert.NotNull(ex.InnerException);
        }

        [Fact]
        public async Task UpdateAsync_NullFieldValue_SkipsUniquenessCheck()
        {
            // Arrange
            var repo = CreateRepoMock();
            var updateChecks = new List<UpdateUniquenessCheck<TestEntity, UpdateTestEntity>>
            {
                new()
                {
                    FieldName = "Name",
                    ValueSelector = dto => dto.Name,  // returns null
                    CurrentValueSelector = entity => entity.Name,
                    FindByUniqueFieldAsync = _ => throw new Exception("should not be called")
                }
            };
            var config = BuildConfig(updateChecks: updateChecks);
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "Original" };
            repo.Setup(r => r.GetByIdTrackedAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var dto = new UpdateTestEntity { Name = null }; // null → skip check

            // Act
            var result = await service.UpdateAsync(TestId, dto);

            // Assert
            Assert.Same(existing, result);
            Assert.Equal("Original", result.Name); // unchanged
        }

        [Fact]
        public async Task UpdateAsync_SameValue_SkipsUniquenessCheck()
        {
            // Arrange
            var repo = CreateRepoMock();
            var updateChecks = new List<UpdateUniquenessCheck<TestEntity, UpdateTestEntity>>
            {
                new()
                {
                    FieldName = "Name",
                    ValueSelector = dto => dto.Name,
                    CurrentValueSelector = entity => entity.Name,
                    FindByUniqueFieldAsync = _ => throw new Exception("should not be called")
                }
            };
            var config = BuildConfig(updateChecks: updateChecks);
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "SameName" };
            repo.Setup(r => r.GetByIdTrackedAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var dto = new UpdateTestEntity { Name = "SameName" }; // same as current → skip check

            // Act
            var result = await service.UpdateAsync(TestId, dto);

            // Assert
            Assert.Same(existing, result);
            Assert.Equal("SameName", result.Name);
        }
    }

    #endregion

    #region Delete

    public class Delete
    {
        [Fact]
        public async Task DeleteAsync_EntityExists_DeletesSuccessfully()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "ToDelete" };
            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            await service.DeleteAsync(TestId);

            // Assert
            repo.Verify(r => r.Delete(existing), Times.Once);
            repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_EntityNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync((TestEntity?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => service.DeleteAsync(TestId));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal(TestId, ex.Key);
        }

        [Fact]
        public async Task DeleteAsync_RowsAffectedZero_ThrowsEntityServiceException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "ToDelete" };
            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityServiceException>(() => service.DeleteAsync(TestId));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Contains("Delete", ex.Operation);
        }

        [Fact]
        public async Task DeleteAsync_ForeignKeyViolationWithResolver_ThrowsEntityConflictException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var innerException = new Exception("FOREIGN KEY constraint failed");
            var dbUpdateException = new DbUpdateException("Test error", innerException);

            var config = BuildConfig(resolveFk: ex => ("schedules", "Cannot delete: entity has schedules"));
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "HasDeps" };
            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityConflictException>(() => service.DeleteAsync(TestId));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal("schedules", ex.ConflictType);
            Assert.Equal("Cannot delete: entity has schedules", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_ForeignKeyViolationNullResolver_ThrowsEntityConflictExceptionWithDefaults()
        {
            // Arrange
            var repo = CreateRepoMock();
            var innerException = new Exception("FOREIGN KEY constraint failed");
            var dbUpdateException = new DbUpdateException("Test error", innerException);

            var config = BuildConfig(resolveFk: null);
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "HasDeps" };
            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityConflictException>(() => service.DeleteAsync(TestId));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal("dependencies", ex.ConflictType);
            Assert.Contains("Cannot delete", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_ForeignKeyViolationResolverReturnsNull_ThrowsEntityConflictExceptionWithDefaults()
        {
            // Arrange
            var repo = CreateRepoMock();
            var innerException = new Exception("FOREIGN KEY constraint failed");
            var dbUpdateException = new DbUpdateException("Test error", innerException);

            var config = BuildConfig(resolveFk: _ => null);
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "HasDeps" };
            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityConflictException>(() => service.DeleteAsync(TestId));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Equal("dependencies", ex.ConflictType);
            Assert.Contains("Cannot delete", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_RepositoryThrows_ThrowsEntityServiceException()
        {
            // Arrange
            var repo = CreateRepoMock();
            var config = BuildConfig();
            var logger = CreateLoggerMock();
            var service = new CrudService<TestEntity, CreateTestEntity, UpdateTestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "ToDelete" };
            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new Exception("unexpected"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityServiceException>(() => service.DeleteAsync(TestId));
            Assert.Equal(EntityName, ex.EntityName);
            Assert.Contains("Delete", ex.Operation);
            Assert.NotNull(ex.InnerException);
        }
    }

    #endregion

    #region Section variant (TEntity = TCreate = TUpdate)

    /// <summary>
    /// Tests using a Section-style config where entity, create DTO, and update DTO
    /// are all the same type. This pattern is used for entities that are simple enough
    /// to share their type across all operations.
    /// </summary>
    public class SectionVariant
    {
        [Fact]
        public async Task CreateAsync_EntityAsDto_GeneratesNewGuid_WhenIdEmpty()
        {
            // Arrange
            var repo = new Mock<IGenericCrudRepository<TestEntity>>();
            var config = BuildSectionConfig();
            var logger = CreateSectionLoggerMock();
            var service = new CrudService<TestEntity, TestEntity, TestEntity>(repo.Object, config, logger.Object);

            var dto = new TestEntity { Id = Guid.Empty, Name = "NewSection" };
            TestEntity? capturedEntity = null;
            repo.Setup(r => r.CreateAsync(It.IsAny<TestEntity>()))
                .Callback<TestEntity>(e => capturedEntity = e)
                .ReturnsAsync((TestEntity e) => e);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.NotEqual(Guid.Empty, capturedEntity!.Id);
            Assert.Equal("NewSection", capturedEntity.Name);
        }

        [Fact]
        public async Task CreateAsync_EntityAsDto_PreservesProvidedId_WhenNotEmpty()
        {
            // Arrange
            var repo = new Mock<IGenericCrudRepository<TestEntity>>();
            var config = BuildSectionConfig();
            var logger = CreateSectionLoggerMock();
            var service = new CrudService<TestEntity, TestEntity, TestEntity>(repo.Object, config, logger.Object);

            var providedId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var dto = new TestEntity { Id = providedId, Name = "NewSection" };
            TestEntity? capturedEntity = null;
            repo.Setup(r => r.CreateAsync(It.IsAny<TestEntity>()))
                .Callback<TestEntity>(e => capturedEntity = e)
                .ReturnsAsync((TestEntity e) => e);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.Equal(providedId, capturedEntity!.Id);
            Assert.Equal("NewSection", capturedEntity.Name);
        }

        [Fact]
        public async Task UpdateAsync_EmptyUniquenessChecks_SkipsPreSaveValidation()
        {
            // Arrange
            var repo = new Mock<IGenericCrudRepository<TestEntity>>();
            var config = BuildSectionConfig();
            var logger = CreateSectionLoggerMock();
            var service = new CrudService<TestEntity, TestEntity, TestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "OldSection" };
            repo.Setup(r => r.GetByIdTrackedAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

            var dto = new TestEntity { Name = "NewSection" };

            // Act
            var result = await service.UpdateAsync(TestId, dto);

            // Assert
            Assert.Same(existing, result);
            Assert.Equal("NewSection", result.Name);
            repo.Verify(r => r.Update(existing), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ForeignKeyViolation_NullResolver_ThrowsEntityConflictException()
        {
            // Arrange
            var repo = new Mock<IGenericCrudRepository<TestEntity>>();
            var innerException = new Exception("FOREIGN KEY constraint failed");
            var dbUpdateException = new DbUpdateException("Test error", innerException);

            var config = BuildSectionConfig(resolveFk: null);
            var logger = CreateSectionLoggerMock();
            var service = new CrudService<TestEntity, TestEntity, TestEntity>(repo.Object, config, logger.Object);

            var existing = new TestEntity { Id = TestId, Name = "HasDeps" };
            repo.Setup(r => r.GetByIdAsync(TestId)).ReturnsAsync(existing);
            repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityConflictException>(() => service.DeleteAsync(TestId));
            Assert.Equal("Section", ex.EntityName);
            Assert.Equal("dependencies", ex.ConflictType);
        }
    }

    #endregion
}
