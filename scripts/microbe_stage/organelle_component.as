abstract class OrganelleComponent{


}

class PlacedOrganelleComponent{

    PlacedOrganelleComponent(OrganelleComponent@ organelleComponent){

        @this.organelleComponent = organelleComponent;
    }

    OrganelleComponent@ organelleComponent;
}










