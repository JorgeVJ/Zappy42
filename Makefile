MAKEFLAGS				:=	--no-print-directory
## ANSI
CSI						:=	\033[
FOREGROUND				:=	3
BACKGROUND				:=	4
BLINK					:=	5
UNBLINK					:=	25
FOREGROUND_BRIGHT		:=	9
BACKGROUND_BRIGHT		:=	10 FORE/BACKGROUND_SET		:=	8 #red_number 0-255;green_number 0-255; blue_number 0-255; RGB						:=	;2;#255;0;0 3-4BITS					:=	:5:#nm
BLACK					:=	0
RED						:=	1
GREEN					:=	2
YELLOW					:=	3
BLUE					:=	4
MAGENTA					:=	5
LIGHT_BLUE				:=	6
END						:=	m
#END ANSI
SHELL					:=	/bin/sh
# ALL RULES NAME FOR

NAME_CLIENT		:=  client
NAME_SERVER   :=  server
CORE-DIR      := ./Core/
CLIENT-DIR    := ./Client/
SERVER-DIR    := ./Server/
BUILD-DIR			:=	./.build/
TESTS-DIR			:=	./Tests/

# GUI (Monitor Godot 4.6 mono / C#)
GUI-DIR         := ./Monitor
GODOT           ?= $(HOME)/godot-mono/godot
DOTNET-LOCAL    := $(GUI-DIR)/.dotnet
DOTNET-CHANNEL  := 8.0
DOTNET-INSTALL  := https://dot.net/v1/dotnet-install.sh

TEST_GETOPT_DIR      		 := $(TESTS-DIR)GetOpt/
TEST_GETOPT_SERVER	     := $(TEST_GETOPT_DIR)server
TEST_GETOPT_CLIENT	     := $(TEST_GETOPT_DIR)client
TEST_VALIDATORS_DIR      := $(TESTS-DIR)Validators/
TEST_VALIDATORS_SERVER   := $(TEST_VALIDATORS_DIR)server


SRC-CORE			:=  Connection \
									ZappySocket \
									Tile \
									CommandHistory \
									CommandEntry \
									Direction \
									Player \
									Core \
									pch \
									utils \
									Inventory \
									Map \
									Point \
									CommandType \
									GetOpt \
									validators \



SRC-CLIENT := \
  $(addprefix Client/, main IAgent responses AgentBreeder AgentExplorer \
    AgentChaman AgentFeeder AgentStoner ExplorationService \
    InfluenceService Bid Blackboard ClientGame) \
  $(addprefix Core/, $(SRC-CORE))

SRC-SERVER-NO-MAIN := \
	  $(addprefix Server/, Game events responses servervalidators \
		ArgValidation ServerSimple TeamManager SocketManager) \
	  $(addprefix Core/, $(SRC-CORE))

SRC-SERVER := \
  $(addprefix Server/, main) $(SRC-SERVER-NO-MAIN)


TEST_GETOPT_SRC_SERVER := $(TEST_GETOPT_DIR)server.cpp \
                          $(addsuffix .o, $(addprefix $(BUILD-DIR), $(SRC-SERVER-NO-MAIN)))


TEST_GETOPT_SRC_CLIENT := $(TEST_GETOPT_DIR)client.cpp \
                          $(addsuffix .o, $(addprefix $(BUILD-DIR), $(addprefix Core/, $(SRC-CORE))))

TEST_VALIDATORS_SRC_SERVER := $(TEST_VALIDATORS_DIR)server.cpp \
                          $(addsuffix .o, $(addprefix $(BUILD-DIR), $(SRC-SERVER-NO-MAIN)))


BUILD-CLIENT := $(addsuffix .o, $(addprefix $(BUILD-DIR),  $(SRC-CLIENT)))
# Same as below BUILD-SERVER := $(SRC-SERVER:%.cpp=$(BUILD-DIR)%.o)
BUILD-SERVER := $(addsuffix .o, $(addprefix $(BUILD-DIR),  $(SRC-SERVER)))

# Set the CXX variable based on the operating system
UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S),Linux)
    # Check for Guix
    ifeq ($(shell grep -q 'guix' /etc/os-release && echo yes),yes)
        CXX := g++
    else
        CXX := g++
    endif
else
    $(error Unsupported operating system: $(UNAME_S))
endif

CXXFLAGS			:= -Wall -Werror -Wextra -g3 --std=c++20 # -fsanitize=address
CXXFLAGS			+= -I $(CORE-DIR) -I $(SERVER-DIR) -I $(CLIENT-DIR)
RM						:=	rm -rf

# For create directory and print
DIR_DUP					=	mkdir -p $(@D)
END-RULE				=	@echo "$(CSI)$(BLINK)$(END)🎉🎊$(CSI)$(UNBLINK)$(END)$(CSI)$(FOREGROUND)$(GREEN)$(END) $@ $(CSI)$(END)$(CSI)$(BLINK)$(END)🎊$(CSI)$(UNBLINK)$(END)"
# RULES

all: $(NAME_SERVER) $(NAME_CLIENT)

$(NAME_CLIENT): $(BUILD-CLIENT)
	$(CXX) $(CXXFLAGS) -o $@ $(BUILD-CLIENT)

$(NAME_SERVER): $(BUILD-SERVER)
	$(CXX) $(CXXFLAGS) -o $@ $(BUILD-SERVER)

test_get_opt: $(TEST_GETOPT_SERVER) $(TEST_GETOPT_CLIENT)
	@echo "$(CSI)$(FOREGROUND)$(GREEN)$(END)Running GetOpt tests$(CSI)$(END)"
	@cd $(TEST_GETOPT_DIR) && bash ./test.sh

test_validators: $(TEST_VALIDATORS_SERVER)
	@echo "$(CSI)$(FOREGROUND)$(GREEN)$(END)Running Validators tests$(CSI)$(END)"
	@cd $(TEST_VALIDATORS_DIR) && bash ./test.sh

$(TEST_VALIDATORS_SERVER): $(TEST_VALIDATORS_SRC_SERVER)
	$(CXX) $(CXXFLAGS) $^ -o $@


$(TEST_GETOPT_SERVER): $(TEST_GETOPT_SRC_SERVER)
	$(CXX) $(CXXFLAGS) $^ -o $@

$(TEST_GETOPT_CLIENT): $(TEST_GETOPT_SRC_CLIENT)
	$(CXX) $(CXXFLAGS) $^ -o $@


$(BUILD-DIR)%.o:        %.cpp
	@$(DIR_DUP)
	$(CXX) $(CXXFLAGS) -c $< -o $@

gui: gui-dotnet
	@if ! command -v "$(GODOT)" >/dev/null 2>&1; then \
		echo "Error: no se encontro el binario de Godot ('$(GODOT)'). Instalalo o: make gui GODOT=/ruta/a/godot"; \
		exit 1; \
	fi; \
	if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^8\.'; then \
		DOTNET_DIR="$$(dirname "$$(command -v dotnet)")"; \
	elif [ -x "$(abspath $(DOTNET-LOCAL))/dotnet" ]; then \
		DOTNET_DIR="$(abspath $(DOTNET-LOCAL))"; \
	else \
		echo "Error: no hay .NET SDK 8 disponible"; exit 1; \
	fi; \
	export PATH="$$DOTNET_DIR:$$PATH"; \
	export DOTNET_ROOT="$$DOTNET_DIR"; \
	export DOTNET_CLI_TELEMETRY_OPTOUT=1; \
	export DOTNET_NOLOGO=1; \
	echo "Importando recursos..."; \
	"$(GODOT)" --path "$(GUI-DIR)" --headless --import; \
	echo "Compilando solucion C#..."; \
	"$(GODOT)" --path "$(GUI-DIR)" --headless --build-solutions --quit; \
	echo "GUI lista. Lanzar con: $(GODOT) --path $(GUI-DIR) [--mock | -h <host> -p <port>]"

gui-dotnet:
	@if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^8\.'; then \
		echo "dotnet 8 del sistema: $$(command -v dotnet)"; \
	elif [ -x "$(DOTNET-LOCAL)/dotnet" ]; then \
		echo "dotnet local ya instalado en $(DOTNET-LOCAL)"; \
	else \
		echo "dotnet no encontrado. Instalando .NET SDK $(DOTNET-CHANNEL) en $(DOTNET-LOCAL) (sin sudo)..."; \
		mkdir -p "$(DOTNET-LOCAL)"; \
		if command -v curl >/dev/null 2>&1; then \
			curl -fsSL "$(DOTNET-INSTALL)" -o "$(DOTNET-LOCAL)/dotnet-install.sh"; \
		elif command -v wget >/dev/null 2>&1; then \
			wget -qO "$(DOTNET-LOCAL)/dotnet-install.sh" "$(DOTNET-INSTALL)"; \
		else \
			echo "Error: se necesita curl o wget para descargar dotnet-install.sh"; exit 1; \
		fi; \
		bash "$(DOTNET-LOCAL)/dotnet-install.sh" --channel $(DOTNET-CHANNEL) --install-dir "$(DOTNET-LOCAL)"; \
		echo ".NET SDK instalado en $(DOTNET-LOCAL)"; \
	fi

gui-fclean:
	@$(RM) $(GUI-DIR)/.godot $(GUI-DIR)/.dotnet $(GUI-DIR)/dotnet-install.sh

clean:
	@$(RM) $(BUILD-DIR) $(TEST_GETOPT_SERVER) $(TEST_GETOPT_CLIENT) $(TEST_GETOPT_DIR)err.log $(TEST_GETOPT_DIR)out.log $(TEST_VALIDATORS_SERVER)

fclean: clean
	@$(RM) $(NAME_CLIENT) $(NAME_SERVER)

re: fclean all

info-%:
	@$(MAKE) --dry-run --always-make $* | grep -v "info"

print-%:
	@$(info '$*'='$($*)')

.PHONY: all clean fclean re info-% print-% test_getopt test_validators gui gui-dotnet gui-fclean
