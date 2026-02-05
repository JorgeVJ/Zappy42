#include "AgentChaman.h"
#include "Inventory.h"
#include "Bid.h"

void AgentChaman::GetBids(Blackboard& bb)
{
    auto it = bb.Me.inventory.IncantationRecipes.find(bb.Me.Level);
    if (it == bb.Me.inventory.IncantationRecipes.end())
        return; // no se puede incantar a este nivel

    const Inventory& recipe = it->second;

    if (!bb.Me.inventory.Has(recipe))
        return; // No se añaden Bids ya que no es posible hacer la encantación.

    // TODO: Revisar los mensajes recibidos en el bb.
    // TODO: Revisar si existen los jugadores en el mismo Tile.

    // else enviar broadcast
    bb.Bids.push_back(Bid("broadcast 2", bb.Me.Level <= 1 ? 15.0 : 0.0));

    // Si todo cumple (Recipe + Jugadores), Bid(Incantation);
}

AgentChaman::~AgentChaman() {};
