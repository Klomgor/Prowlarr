import React from 'react';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';
import translate from 'Utilities/String/translate';
import styles from '../styles.css';

function Donations() {
  return (
    <FieldSet legend={translate('Donations')}>
      <div className={styles.logoContainer} title="Radarr">
        <Link to="https://radarr.video/donate">
          <img
            className={styles.logo}
            src={`${window.Prowlarr.urlBase}/Content/Images/Icons/logo-radarr.png`}
          />
        </Link>
      </div>

      <div className={styles.logoContainer} title="Lidarr">
        <Link to="https://lidarr.audio/donate">
          <img
            className={styles.logo}
            src={`${window.Prowlarr.urlBase}/Content/Images/Icons/logo-lidarr.png`}
          />
        </Link>
      </div>

      <div className={styles.logoContainer} title="Readarr">
        <Link to="https://readarr.com/donate">
          <img
            className={styles.logo}
            src={`${window.Prowlarr.urlBase}/Content/Images/Icons/logo-readarr.png`}
          />
        </Link>
      </div>

      <div className={styles.logoContainer} title="Prowlarr">
        <Link to="https://prowlarr.com/donate">
          <img
            className={styles.logo}
            src={`${window.Prowlarr.urlBase}/Content/Images/Icons/logo-prowlarr.png`}
          />
        </Link>
      </div>

      <div className={styles.logoContainer} title="Sonarr">
        <Link to="https://opencollective.com/sonarr">
          <img
            className={styles.logo}
            src={`${window.Prowlarr.urlBase}/Content/Images/Icons/logo-sonarr.png`}
          />
        </Link>
      </div>
    </FieldSet>
  );
}

export default Donations;
