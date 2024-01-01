import * as React from 'react';
import ConfigProvider from '../services/ConfigProvider';
import Cookies from 'universal-cookie';

export function CookieModal() {

    const privacyPolicy = () => {
        const config = ConfigProvider.getIdentityConfig()
        return config.authority + '/policy/dancedanceapp'
    }

    const onCookiesClose = () => {
        const cookies = new Cookies();
        cookies.set('tbdancedanceappcookiesaccepted', true)
        const element = document.getElementById('cookiesmodal')
        element?.classList.add('is-hidden')
    }

    React.useEffect(() => {
        const cookies = new Cookies();
        const val = cookies.get('tbdancedanceappcookiesaccepted') as boolean
        if (val != true) {
            const element = document.getElementById('cookiesmodal')
            element?.classList.remove('is-hidden')
        }
    }, [])

    return (
        <div id='cookiesmodal' className="notification is-hidden">
            <p>Hej! Aplikacja uzyskuje dostęp i przechowujemy informacje na urządzeniu oraz przetwarza dane osobowe, takie jak unikalne identyfikatory i standardowe informacje wysyłane przez urządzenie czy dane przeglądania w celu świadczenia usług! Więcej informacji znajdziesz tutaj: <a href={privacyPolicy()}>Polityka Prywatności</a></p>
            <button aria-label="close" className="button" onClick={onCookiesClose}>Ok, Akceptuję!</button>
        </div>
    );
}
