# Makefile for Attendance Management Backend

# Variables
DOTNET := dotnet
SOLUTION := attendance.sln
APP_PROJECT := attendance_monitoring/attendance_monitoring.csproj
TEST_PROJECT := attendance.testproject/attendance.testproject.csproj

# Default target
.PHONY: help
help: ## Show this help message
	@echo "Attendance Management Backend - Makefile"
	@echo ""
	@echo "Usage:"
	@echo "  make run                    - Run the application"
	@echo "  make test                   - Run all tests"
	@echo "  make test-specific          - Run specific test class (StudentControllerTest)"
	@echo "  make build                  - Build the solution"
	@echo "  make check                  - Run build and test"
	@echo "  make clean                  - Clean build artifacts"
	@echo "  make restore                - Restore NuGet packages"
	@echo ""

.PHONY: run
run: ## Run the application
	$(DOTNET) run --project $(APP_PROJECT)

.PHONY: test
test: ## Run all tests
	$(DOTNET) test $(SOLUTION)

.PHONY: test-specific
test-specific: ## Run specific test class
	$(DOTNET) test --filter "ClassName=StudentControllerTest" --configuration Debug

.PHONY: build
build: ## Build the solution
	$(DOTNET) build $(SOLUTION)

.PHONY: check
check: ## Run build and test
	make build
	make test

.PHONY: clean
clean: ## Clean build artifacts
	$(DOTNET) clean $(SOLUTION)
	rm -rf ./attendance_monitoring/bin ./attendance_monitoring.obj
	rm -rf ./attendance.testproject/bin ./attendance.testproject/obj

.PHONY: restore
restore: ## Restore NuGet packages
	$(DOTNET) restore $(SOLUTION)