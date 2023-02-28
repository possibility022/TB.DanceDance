import * as React from 'react';
import { useState } from 'react';
import videoInfoService from '../../services/VideoInfoService';
import { EventType } from '../../types/EventType';
import { IAssignedEventSharingScopeModel, ISharingScopeModel } from '../../types/SharingScopeModel';
import { Dropdown } from '../Dropdown';
import { IItemToSelect, SelectableList } from './SelectableList';

export interface IAccessToVideoRequestFormProps {
    placeholder: string
}

export function AccessToVideoRequestForm(props: IAccessToVideoRequestFormProps) {

    const [events, setEvents] = useState<Array<IItemToSelect<string>>>([])
    const [workshops, setWorkshops] = useState<Array<IItemToSelect<string>>>([])

    const [availableGroupNames, setAvailableGroupNames] = useState<Array<string>>([])

    let allSharingScopes: {events: Array<IAssignedEventSharingScopeModel>, groups: Array<ISharingScopeModel>}
    const selectedScopes = new Map<string, boolean>()

    const mapToItemToSelect = (item: IAssignedEventSharingScopeModel) => {
        const itemToSelect: IItemToSelect<string> = {
            key: item.id,
            text: item.name,
            selectionDisabled: item.isAssigned
        }

        return itemToSelect
    }

    React.useEffect(() => {
        videoInfoService.GetAvailableEventsAndGroups()
            .then(sharringScopes => {

                allSharingScopes = sharringScopes

                const eventsToSet = new Array<IItemToSelect<string>>()
                const workshopsToSet = new Array<IItemToSelect<string>>()

                for (const el of sharringScopes.events) {
                    const mapped = mapToItemToSelect(el)
                    if (el.type == EventType.SmallWorkshop)
                        workshopsToSet.push(mapped)
                    else {
                        eventsToSet.push(mapped)
                    }
                }

                setEvents(eventsToSet)
                setWorkshops(workshopsToSet)

                setAvailableGroupNames(sharringScopes.groups.map(r => r.name))
            })
            .catch(e => console.error(e))
    }, [])

    const groupSelected = (item: IItemToSelect<string>, isSelected: boolean) => {
        selectedScopes.set(item.key, isSelected)
    }

    return (
        <div>
            <div className="content">
                <h1>Wybierz grupy West Coast Swing!</h1>
                <h2>Wybierz swoją podstawową grupę do której regularnie uczęszczasz na zajęcia.</h2>
                <p>Dodatkowo, możesz wybrać wydarzenia na jakich byłeś i gdzie brałeś udział. Wydarzenia np. takie jak Karnawał WestLove, Baltic Swing, Halloween.</p>
                <p>Wybierz również warsztaty w jakich brałeś udział. Są to mniejsze wydarzenia.</p>
                <p>Pamietaj, że Twój wybór będzie weryfikowany!</p>
            </div>
            <div className='columns'>
                <div className='column is-full'>
                    <Dropdown isLoading={false} items={availableGroupNames} unselectedText={"Wybierz grupę"} selectedItemIndex={0} startWithUnselected={true} />
                </div>
            </div>
            <div className="columns">
                <div className="column">
                    <SelectableList<string>
                        articleClassName='is-info'
                        header='Wydarzenia'
                        onItemStatusChange={groupSelected}
                        text='Wybierz wydarzenia w których brałeś udział!'
                        options={events} />
                </div>
                <div className="column">
                    <SelectableList<string>
                        articleClassName='is-warning'
                        header='Warsztaty'
                        onItemStatusChange={groupSelected}
                        text='Wybierz warsztaty gdzie próbowałaś swoich sił!'
                        options={workshops} />
                </div>
            </div>
        </div>
    );
}
