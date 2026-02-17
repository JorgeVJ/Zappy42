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

SRC-CORE			:=  Connection \
									Tile \
									CommandHistory \
									Core \
									pch \
									utils \
									Inventory \
									Map \
									Point \
									CommandType \


SRC-CLIENT := \
  $(addprefix Client/, main IAgent AgentBreeder AgentExplorer \
    AgentChaman AgentHungry AgentStoner ExplorationService \
    InfluenceService Bid Blackboard ClientGame) \
  $(addprefix Core/, $(SRC-CORE))

SRC-SERVER := \
  $(addprefix Server/, main Game events responses) \
  $(addprefix Core/, $(SRC-CORE))

BUILD-SERVER := $(SRC-SERVER:%.cpp=$(BUILD-DIR)%.o)

BUILD-CLIENT := $(addsuffix .o, $(addprefix $(BUILD-DIR),  $(SRC-CLIENT)))
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

CXXFLAGS			:= -Wall -Werror -Wextra -g3# -fsanitize=address
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

$(BUILD-DIR)%.o:        %.cpp
	@$(DIR_DUP)
	$(CXX) $(CXXFLAGS) -c $< -o $@

clean:
	@$(RM) $(BUILD-DIR)

fclean: clean
	@$(RM) $(NAME_CLIENT) $(NAME_SERVER)

re: fclean all

info-%:
	@$(MAKE) --dry-run --always-make $* | grep -v "info"

print-%:
	@$(info '$*'='$($*)')

.PHONY: all clean fclean re info-% print-%
