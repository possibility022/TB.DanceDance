import * as React from 'react';
import { Dropdown } from '../Dropdown';
import { IItemToSelect, SelectableList } from './SelectableList';

export interface IAccessToVideoRequestFormProps {
    placeholder: string
}

export function AccessToVideoRequestForm(props: IAccessToVideoRequestFormProps) {


    const events: Array<string> = ['Event a', 'Event a', 'Event a', 'Event a', 'Event a', 'Event a', 'Event a', 'Event a', 'Event a', 'Event a']
    const workshops: Array<string> = ['Workshop ABC ABC ABC', 'Workshop ABC ABC ABC', 'Workshop ABC ABC ABC', 'Workshop ABC ABC ABC', 'Workshop ABC ABC ABC', 'Workshop ABC ABC ABC']

    const eventSelected = (item: IItemToSelect) => {
        console.log(item)
    }

    const workshopSelected = (item: IItemToSelect) => {
        console.log(item)
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
                    <Dropdown isLoading={false} items={['x']} unselectedText={"Wybierz grupę"} selectedItemIndex={0} startWithUnselected={true} />
                </div>
            </div>
            <div className="columns">
                <div className="column">
                    <SelectableList
                        articleClassName='is-info'
                        header='Wydarzenia'
                        onItemSelected={eventSelected}
                        text='Wybierz wydarzenia w których brałeś udział!'
                        options={[]} />
                </div>
                <div className="column">
                    <SelectableList
                        articleClassName='is-warning'
                        header='Warsztaty'
                        onItemSelected={workshopSelected}
                        text='Wybierz warsztaty gdzie próbowałaś swoich sił!'
                        options={[]} />
                </div>
            </div>
        </div>
    );
}
