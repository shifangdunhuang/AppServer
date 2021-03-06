import React, { useCallback } from "react";
import { withRouter } from "react-router";
import { RowContent, Link, LinkWithDropdown, Icons, Text } from "asc-web-components";
import { connect } from "react-redux";
import { getUserStatus } from "../../../../../store/people/selectors";

const getFormatedGroups = (user, status, selectGroup) => {
  let temp = [];
  const groups = user.groups;
  const linkColor = "#333";

  if (!groups) temp.push({ key: 0, label: '' });

  groups && groups.map(group =>
    temp.push(
      {
        key: group.id,
        label: group.name,
        onClick: () => selectGroup(group.id)
      }
    )
  );

  if (temp.length <= 1) {
    return (
      <Link
        isTextOverflow={true}
        containerWidth='25%'
        type='action'
        title={temp[0].label}
        fontSize='12px'
        fontWeight={400}
        color={linkColor}
        onClick={temp[0].onClick}
      >
        {temp[0].label}
      </Link>);
  } else {
    return (
      <LinkWithDropdown
        isTextOverflow={true}
        containerWidth='25%'
        title={temp[0].label}
        fontSize='12px'
        fontWeight={400}
        color={linkColor}
        data={temp}
      >
        {temp[0].label}
      </LinkWithDropdown>);
  }
};

const UserContent = ({ user, history, settings, selectGroup }) => {
  const { userName, displayName, title, mobilePhone, email } = user;
  const status = getUserStatus(user);
  const groups = getFormatedGroups(user, status, selectGroup);

  const onUserNameClick = useCallback(
    (e) => {
      e.preventDefault();
      history.push(`${settings.homepage}/view/${userName}`)
    },
    [history, settings.homepage, userName]
  );

  const onPhoneClick = useCallback(
    () => window.open(`sms:${mobilePhone}`),
    [mobilePhone]
  );

  const onEmailClick = useCallback(
    () => window.open(`mailto:${email}`),
    [email]
  );

  const nameColor = "#333";
  const sideInfoColor = "#333";

  const headDepartmentStyle = {
    maxWidth: '1fr'
  }

  return (
    <RowContent
      sideColor={sideInfoColor}
    >
      <Link
        containerWidth='28%'
        type='page'
        href={`/products/people/view/${userName}`}
        title={displayName}
        fontWeight="bold"
        onClick={onUserNameClick}
        fontSize='15px'
        color={nameColor}
        isTextOverflow={true}
      >
        {displayName}
      </Link>
      <>
        {status === 'pending' && <Icons.SendClockIcon size='small' isfill={true} color='#3B72A7' />}
        {status === 'disabled' && <Icons.CatalogSpamIcon size='small' isfill={true} color='#3B72A7' />}
      </>
      {title
        ?
        <Text
          containerWidth='30%'
          style={headDepartmentStyle}
          as="div"
          color={sideInfoColor}
          fontSize='12px'
          fontWeight={600}
          title={title}
          truncate={true}
        >
          {title}
        </Text>
        : <div containerWidth='30%' style={headDepartmentStyle}></div>
      }
      {groups}
      <Link
        containerWidth='5%'
        type='page'
        title={mobilePhone}
        fontSize='12px'
        fontWeight={400}
        color={sideInfoColor}
        onClick={onPhoneClick}
        isTextOverflow={true}
      >
        {mobilePhone}
      </Link>
      <Link
        containerWidth='15%'
        type='page'
        title={email}
        fontSize='12px'
        fontWeight={400}
        color={sideInfoColor}
        onClick={onEmailClick}
        isTextOverflow={true}
      >
        {email}
      </Link>
    </RowContent>
  );
};

function mapStateToProps(state) {
  return {
    settings: state.auth.settings
  };
}

export default connect(mapStateToProps)(withRouter(UserContent));
