# Makefile for Attendance Management Backend

# Variables
DOTNET := dotnet
SOLUTION := attendance.sln
APP_PROJECT := attendance_monitoring/attendance_monitoring.csproj
TEST_PROJECT := attendance.testproject/attendance.testproject.csproj
ARCHITECTURE_GUARDRAIL_FILTER := FullyQualifiedName~Architecture_Testing|FullyQualifiedName~RoleAuthorizationGuardrailTests|FullyQualifiedName~SessionStatusGuardrailTests
CRITICAL_VERIFICATION_FILTER := FullyQualifiedName~Integration_Testing|FullyQualifiedName~AccountControllerTest|FullyQualifiedName~AttendanceConcurrencyTests|FullyQualifiedName~QrCodeControllerTest

# Default target
.PHONY: help
help: ## Show this help message
	@echo "Attendance Management Backend - Makefile"
	@echo ""
	@echo "Usage:"
	@echo "  make run                    - Run the application"
	@echo "  make test                   - Run all tests"
	@echo "  make test-integration-sqlserver - Run SQL Server-backed integration tests via Podman wrapper"
	@echo "  make verify-profile-uuid-predeploy - Run the UUID predeploy anomaly gate"
	@echo "  make test-architecture-guardrails - Run architecture and policy guardrail suites"
	@echo "  make test-critical-verification   - Run explicit auth, attendance, QR, and integration verification suites"
	@echo "  make test-specific          - Run specific test class (StudentControllerTest)"
	@echo "  make build                  - Build the solution"
	@echo "  make check                  - Run build, explicit guardrails, critical verification, and full tests"
	@echo "  make clean                  - Clean build artifacts"
	@echo "  make restore                - Restore NuGet packages"
	@echo ""

.PHONY: run
run: ## Run the application
	$(DOTNET) run --project $(APP_PROJECT)

.PHONY: test
test: ## Run all tests
	$(DOTNET) test $(SOLUTION)

.PHONY: test-integration-sqlserver
test-integration-sqlserver: ## Run SQL Server-backed integration tests via Podman wrapper
	./scripts/run-sqlserver-integration-tests.sh

.PHONY: verify-profile-uuid-predeploy
verify-profile-uuid-predeploy: ## Run the UUID predeploy anomaly gate
	./scripts/verify-profile-uuid-predeploy.sh

.PHONY: test-architecture-guardrails
test-architecture-guardrails: build ## Run architecture and policy guardrail suites
	$(DOTNET) test $(TEST_PROJECT) --no-restore --no-build --filter "$(ARCHITECTURE_GUARDRAIL_FILTER)"

.PHONY: test-critical-verification
test-critical-verification: build ## Run explicit auth, attendance, QR, and integration verification suites
	$(DOTNET) test $(TEST_PROJECT) --no-restore --no-build --filter "$(CRITICAL_VERIFICATION_FILTER)"

.PHONY: test-specific
test-specific: ## Run specific test class
	$(DOTNET) test --filter "ClassName=StudentControllerTest" --configuration Debug

.PHONY: build
build: ## Build the solution
	$(DOTNET) build $(SOLUTION)

.PHONY: check
check: build test-architecture-guardrails test-critical-verification test ## Run build and required verification gates

.PHONY: clean
clean: ## Clean build artifacts
	$(DOTNET) clean $(SOLUTION)
	rm -rf ./attendance_monitoring/bin ./attendance_monitoring.obj
	rm -rf ./attendance.testproject/bin ./attendance.testproject/obj

.PHONY: restore
restore: ## Restore NuGet packages
	$(DOTNET) restore $(SOLUTION)
